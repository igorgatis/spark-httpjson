package spark.sources.datasourcev2.httpjson

import org.apache.spark.sql.catalyst.InternalRow
import org.apache.spark.sql.connector.catalog._
import org.apache.spark.sql.connector.expressions.Transform
import org.apache.spark.sql.connector.read._
import org.apache.spark.sql.types._
import org.apache.spark.sql.util.CaseInsensitiveStringMap

import java.util
import scala.collection.JavaConverters._

class DefaultSource extends TableProvider {

  override def inferSchema(options: CaseInsensitiveStringMap): StructType =
    StructType(Array.empty[StructField])

  override def getTable(
      unusedStructType: StructType,
      unusedTransforms: Array[Transform],
      options: util.Map[String, String]): Table = {
    val response = new HttpClient().getTable(new CaseInsensitiveStringMap(options))
    new RestBatchTable(response.name, response.schema)
  }
}

class RestBatchTable(
    private val _name: String,
    private val _schema: StructType) extends Table with SupportsRead {

  override def name(): String = _name

  override def schema(): StructType = _schema

  override def capabilities(): util.Set[TableCapability] =
    Set(TableCapability.BATCH_READ).asJava

  override def newScanBuilder(options: CaseInsensitiveStringMap): ScanBuilder =
    () => new RestBatchScan(new util.HashMap[String, String](options), _schema)
}

case class RestBatchScan(
    private val _options: util.HashMap[String, String],
    private val _schema: StructType) extends Scan with Batch with PartitionReaderFactory {

  override def readSchema(): StructType = _schema

  override def toBatch: Batch = this

  override def planInputPartitions(): Array[InputPartition] = {
    new HttpClient()
      .planInputPartitions(new CaseInsensitiveStringMap(_options))
      .partitions.toArray
  }

  override def createReaderFactory(): PartitionReaderFactory = this

  override def createReader(partition: InputPartition): PartitionReader[InternalRow] = {
    new HttpClient().readPartition(new CaseInsensitiveStringMap(_options), _schema, partition)
  }
}

