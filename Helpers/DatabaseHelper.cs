using Microsoft.Data.SqlClient;
using System.Collections.Generic;


namespace Roi_ocr.Helpers
{
    public class DatabaseHelper
    {
        private readonly string _connectionString;

        public DatabaseHelper()
        {

            _connectionString = "Server=MSI_OKAY;Database=BackPack;User Id=sa;Password=Qazwsx1021;TrustServerCertificate=True;";
        }

        public List<(string ImagePath, string ClassName)> GetImagePathsAndClasses()
        {
            var results = new List<(string ImagePath, string ClassName)>();

            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();
                var query = "SELECT image_path, class_name FROM images";
                using (var command = new SqlCommand(query, connection))
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        string imagePath = reader.GetString(0);
                        string className = reader.GetString(1);
                        results.Add((imagePath, className));
                    }
                }
            }

            return results;
        }
    }
}