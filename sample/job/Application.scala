import org.apache.spark.sql.SparkSession

object Application {
    def main(args: Array[String]): Unit = {
        val spark = SparkSession
            .builder
            .appName("App")
            .getOrCreate()

        val df = spark.read
            .format("spark.sources.datasourcev2.httpjson")
            .option("http.url", "http://csharp-service:9999/spark-api")
            .option("http.user", "someusername")
            .option("http.password", "S3cr3t")
            .option("http.gzip", "true")
            .option("http.ignorecertificates", "true")
            .option("http.X-My-Header", "blah")
            .option("myservice.Entity", "Pets")
            .option("myservice.PartitionSize", "1000")
            .option("myservice.MinimumWeightInKg", "5")
            .load()

        df.printSchema
        df.collect.foreach(println)

        spark.stop()
    }
}