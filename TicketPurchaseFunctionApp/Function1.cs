using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;

namespace TicketPurchaseFunctionApp
{
    public class Function1
    {
        private readonly ILogger<Function1> _logger;

        public Function1(ILogger<Function1> logger)
        {
            _logger = logger;
        }

        [Function("Function1")]
        public async Task Run(
            [QueueTrigger("tickethub", Connection = "AzureWebJobsStorage")] string queueMessage)
        {
            _logger.LogInformation($"Received message: {queueMessage}");

            TicketPurchase purchase;
            try
            {
                purchase = JsonSerializer.Deserialize<TicketPurchase>(queueMessage);
                if (purchase == null)
                {
                    _logger.LogError("Failed to deserialize message.");
                    return;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Deserialization error.");
                return;
            }

            string connectionString = Environment.GetEnvironmentVariable("SqlConnectionString");

            string insertQuery = @"
                INSERT INTO dbo.TicketPurchases 
                (ConcertId, Email, Name, Phone, Quantity, CreditCard, Expiration, SecurityCode, Address, City, Province, PostalCode, Country)
                VALUES 
                (@ConcertId, @Email, @Name, @Phone, @Quantity, @CreditCard, @Expiration, @SecurityCode, @Address, @City, @Province, @PostalCode, @Country)";

            try
            {
                using (SqlConnection conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync();
                    using (SqlCommand cmd = new SqlCommand(insertQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@ConcertId", purchase.ConcertId);
                        cmd.Parameters.AddWithValue("@Email", purchase.Email);
                        cmd.Parameters.AddWithValue("@Name", purchase.Name);
                        cmd.Parameters.AddWithValue("@Phone", purchase.Phone);
                        cmd.Parameters.AddWithValue("@Quantity", purchase.Quantity);
                        cmd.Parameters.AddWithValue("@CreditCard", purchase.CreditCard);
                        cmd.Parameters.AddWithValue("@Expiration", purchase.Expiration);
                        cmd.Parameters.AddWithValue("@SecurityCode", purchase.SecurityCode);
                        cmd.Parameters.AddWithValue("@Address", purchase.Address);
                        cmd.Parameters.AddWithValue("@City", purchase.City);
                        cmd.Parameters.AddWithValue("@Province", purchase.Province);
                        cmd.Parameters.AddWithValue("@PostalCode", purchase.PostalCode);
                        cmd.Parameters.AddWithValue("@Country", purchase.Country);

                        int rows = await cmd.ExecuteNonQueryAsync();
                        _logger.LogInformation($"Inserted {rows} record(s) into the database.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SQL insert error.");
            }
        }
    }

    public class TicketPurchase
    {
        public int ConcertId { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        public int Quantity { get; set; }
        public string CreditCard { get; set; }
        public string Expiration { get; set; }
        public string SecurityCode { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Province { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
    }
}
