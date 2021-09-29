val sparkVersion = "3.1.1"

libraryDependencies ++= Seq(
  "org.apache.spark" %% "spark-core" % sparkVersion % Provided,
  "org.apache.spark" %% "spark-sql" % sparkVersion % Provided,
  "org.scala-lang" % "scala-library" % scalaVersion.value % Provided,
)
