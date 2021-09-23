name := "spark-httpjson"

version := "0.1.0"

organization := "github.com/igorgatis"

scalaVersion := "2.12.10"

crossScalaVersions := Seq("2.11.12", "2.12.10")

scalacOptions := Seq("-unchecked", "-deprecation")

val sparkVersion = sys.props.get("spark.testVersion").getOrElse("2.4.8")

// To avoid packaging it, it's Provided below
autoScalaLibrary := false

libraryDependencies ++= Seq(
  //"commons-io" % "commons-io" % "2.8.0",
  //"org.glassfish.jaxb" % "txw2" % "2.3.4",
  //"org.apache.ws.xmlschema" % "xmlschema-core" % "2.2.5",
  //"org.slf4j" % "slf4j-api" % "1.7.30" % Provided,
  //"org.scalatest" %% "scalatest" % "3.2.9" % Test,
  //"com.novocode" % "junit-interface" % "0.11" % Test,
  "org.apache.spark" %% "spark-core" % sparkVersion % Provided,
  "org.apache.spark" %% "spark-sql" % sparkVersion % Provided,
  "org.scala-lang" % "scala-library" % scalaVersion.value % Provided
)

publishMavenStyle := true

pomExtra :=
  <url>https://github.com/igorgatis/spark-httpjson</url>
  <licenses>
    <license>
      <name>Apache License, Version 2.0</name>
      <url>http://www.apache.org/licenses/LICENSE-2.0.html</url>
      <distribution>repo</distribution>
    </license>
  </licenses>
  <scm>
    <url>git@github.com:igorgatis/spark-httpjson.git</url>
    <connection>scm:git:git@github.com:igorgatis/spark-httpjson.git</connection>
  </scm>
  <developers>
    <developer>
      <id>igorgatis</id>
      <name>Igor Gatis</name>
    </developer>
  </developers>

publishTo := {
  val nexus = "https://oss.sonatype.org/"
  if (isSnapshot.value) Some("snapshots" at nexus + "content/repositories/snapshots")
  else Some("releases" at nexus + "service/local/staging/deploy/maven2")
}

credentials += Credentials(
  "Sonatype Nexus Repository Manager",
  "oss.sonatype.org",
  sys.env.getOrElse("USERNAME", ""),
  sys.env.getOrElse("PASSWORD", ""))

resolvers +=
  "GCS Maven Central mirror" at "https://maven-central.storage-download.googleapis.com/maven2/"

parallelExecution in Test := false

// Skip tests during assembly
test in assembly := {}

fork := true

// Prints JUnit tests in output
testOptions in Test := Seq(Tests.Argument(TestFrameworks.JUnit, "-v"))
