package spark.sources.datasourcev2.httpjson

import com.fasterxml.jackson.databind.json.JsonMapper
import com.fasterxml.jackson.module.scala.DefaultScalaModule
import org.apache.spark.sql.catalyst.InternalRow
import org.apache.spark.sql.catalyst.expressions.JsonToStructs
import org.apache.spark.sql.catalyst.util.FailureSafeParser
import org.apache.spark.sql.connector.read.{ InputPartition, PartitionReader }
import org.apache.spark.sql.types.StructType
import org.apache.spark.sql.util.CaseInsensitiveStringMap
import org.apache.spark.unsafe.types.UTF8String

import java.io.{ BufferedReader, InputStream, InputStreamReader }
import java.net.{ HttpURLConnection, URL }
import java.nio.charset.StandardCharsets
import java.util
import java.util.zip.GZIPInputStream
import javax.net.ssl.{ HostnameVerifier, HttpsURLConnection, SSLSession }
import scala.reflect.{ ClassTag, classTag }
import scala.util.Try

class StringMap extends util.HashMap[String, String]

case class GetTableRequest(options: StringMap)

case class GetTableResponse(
    error: String,
    name: String,
    schema: StructType)

case class PlanInputPartitionsRequest(options: StringMap)

case class RestPartition(payload: Array[Byte]) extends InputPartition

case class PlanInputPartitionsResponse(
    error: String,
    partitions: Array[RestPartition])

case class ReadPartitionRequest(
    options: StringMap,
    payload: Array[Byte])

class ReadPartitionResponse(
    private val _schema: StructType,
    private val _connection: HttpURLConnection,
    private val _compressed: Boolean)
  extends PartitionReader[InternalRow] {

  private val _input: InputStream = _connection.getInputStream
  private val _reader: BufferedReader = {
    if (_input.available == 0) {
      null
    } else {
      val in = if (_compressed) new GZIPInputStream(_input) else _input
      new BufferedReader(new InputStreamReader(in))
    }
  }

  private val _parser: FailureSafeParser[UTF8String] =
    JsonToStructs(_schema, Map.empty[String, String], null, Some("UTC")).parser

  private var _json: String = null

  def next(): Boolean = {
    if (_reader != null) {
      _json = _reader.readLine()
    }
    _json != null && !_json.isEmpty
  }

  def get(): InternalRow = {
    if (_json != null && !_json.isEmpty) {
      val itr = _parser.parse(UTF8String.fromString(_json))
      if (itr.hasNext) return itr.next()
    }
    null
  }

  def close() = {
    _input.close()
    _connection.disconnect()
  }
}

class IgnoreCertificate extends HostnameVerifier {
  def verify(hostname: String, session: SSLSession): Boolean = true
}

case class HttpSettings() {
  var url: Option[String] = None
  var user: Option[String] = None
  var password: Option[String] = None
  var ignoreCertificates: Boolean = false
  var gzip: Boolean = false
  val headers: StringMap = new StringMap()
}

object HttpClient {
  private val HttpOptionsPattern = "(http\\.)?(.+)".r

  private val _mapper = JsonMapper.builder()
    .addModule(DefaultScalaModule)
    .build()
}

class HttpClient {
  import HttpClient._

  private def parseOptions(options: CaseInsensitiveStringMap): (HttpSettings, StringMap) = {
    val settings = HttpSettings()
    val otherOptions = new StringMap()
    options.forEach {
      case (option, value) =>
        val HttpOptionsPattern(prefix, suffix) = option
        (prefix, suffix) match {
          case (null, key) => otherOptions.put(key, value)
          case (_, suffix) => suffix match {
            case "url" => settings.url = Some(value)
            case "user" => settings.user = Some(value)
            case "password" => settings.password = Some(value)
            case "ignorecertificates" => settings.ignoreCertificates = Try(value.toBoolean).getOrElse(false)
            case "gzip" => settings.gzip = Try(value.toBoolean).getOrElse(false)
            case prop => settings.headers.put(prop, value)
          }
        }
    }

    (settings.user, settings.password) match {
      case (None, _) | (_, None) => ()
      case (user, pass) =>
        val basicAuth = util.Base64.getEncoder.encodeToString(
          s"${settings.user.get}:${settings.password.get}"
            .getBytes(StandardCharsets.UTF_8))
        settings.headers.put("Authorization", "Basic " + basicAuth)
    }

    if (settings.gzip) {
      settings.headers.put("Accept-Encoding", "gzip")
    }
    (settings, otherOptions)
  }

  private def sendJson(
      settings: HttpSettings,
      path: String, request: Any,
      expectedMime: String): HttpURLConnection = {

    val connection = new URL(s"${settings.url.get}${path}")
      .openConnection.asInstanceOf[HttpURLConnection]
    if (settings.ignoreCertificates && connection.isInstanceOf[HttpsURLConnection]) {
      connection.asInstanceOf[HttpsURLConnection].setHostnameVerifier(new IgnoreCertificate())
    }
    connection.setUseCaches(false)
    connection.setAllowUserInteraction(false)
    connection.setDoOutput(true)
    connection.setRequestMethod("POST")

    settings.headers.forEach {
      case (key, value) => connection.setRequestProperty(key, value)
    }

    val jsonBytes = _mapper.writer().writeValueAsBytes(request)
    connection.setRequestProperty("Content-Length", jsonBytes.length.toString)
    connection.setRequestProperty("Content-Type", "application/json")
    connection.setRequestProperty("Accept", expectedMime)

    val output = connection.getOutputStream
    try {
      output.write(jsonBytes)
    } finally {
      output.close()
    }
    connection.connect()

    val status = connection.getResponseCode
    if (status != 200) {
      throw new Exception(s"Request failed: $status ${connection.getResponseMessage}")
    }
    val responseContentType = connection.getHeaderField("Content-Type")
    if (responseContentType != expectedMime) {
      throw new Exception(s"Response MIME type mismatch, expected=$expectedMime, actual=$responseContentType")
    }
    connection
  }

  private def isResponseGzipped(connection: HttpURLConnection): Boolean = {
    connection.getHeaderFields.forEach {
      case (key, value) => {
        if (key != null && key.toLowerCase() == "content-encoding") {
          value.forEach {
            case item => if (item.toLowerCase() == "gzip") return true
          }
        }
      }
    }
    false
  }

  private def parseJsonResponse[T: ClassTag](connection: HttpURLConnection): T = {
    var input = connection.getInputStream
    if (isResponseGzipped(connection)) {
      input = new GZIPInputStream(input)
    }
    try {
      _mapper.readValue(input, classTag[T].runtimeClass).asInstanceOf[T]
    } finally {
      input.close()
      connection.disconnect()
    }
  }

  def getTable(options: CaseInsensitiveStringMap): GetTableResponse = {
    val (httpSettings, otherOptions) = parseOptions(options)
    val request = GetTableRequest(otherOptions)
    val connection = sendJson(httpSettings, "GetTable", request, "application/json")
    val response = parseJsonResponse[GetTableResponse](connection)
    if (response.error != null) throw new Exception(response.error)
    response
  }

  def planInputPartitions(options: CaseInsensitiveStringMap): PlanInputPartitionsResponse = {
    val (httpSettings, otherOptions) = parseOptions(options)
    val request = PlanInputPartitionsRequest(otherOptions)
    val connection = sendJson(httpSettings, "PlanInputPartitions", request, "application/json")
    val response = parseJsonResponse[PlanInputPartitionsResponse](connection)
    if (response.error != null) throw new Exception(response.error)
    response
  }

  def readPartition(
      options: CaseInsensitiveStringMap,
      schema: StructType,
      partition: InputPartition): ReadPartitionResponse = {
    val (httpSettings, otherOptions) = parseOptions(options)
    val payload = partition.asInstanceOf[RestPartition].payload
    val request = ReadPartitionRequest(otherOptions, payload)
    val connection = sendJson(httpSettings, "ReadPartition", request, "application/json-seq")
    new ReadPartitionResponse(schema, connection, isResponseGzipped(connection))
  }
}
