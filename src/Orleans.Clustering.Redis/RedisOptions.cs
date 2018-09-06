namespace Orleans.Clustering.Redis
{
    public class RedisOptions
    {
        public int Database { get; set; } = 0;
        public string ConnectionString { get; set; } = "localhost:6379";
    }
}