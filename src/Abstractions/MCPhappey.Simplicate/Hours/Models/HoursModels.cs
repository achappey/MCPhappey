using System.Text.Json.Serialization;
using MCPhappey.Simplicate.Extensions;

namespace MCPhappey.Simplicate.Hours.Models;


    public enum ApprovalStatusLabel
    {
        to_approved_project,
        to_forward,
        approved,
        rejected
    }

    public enum InvoiceStatus
    {
        invoiced
    }

    public class SimplicateHourTotals
    {
        [JsonPropertyName("totalHours")]
        public double TotalHours { get; set; }

        [JsonPropertyName("totalAmount")]
        public decimal TotalAmount { get; set; }
    }

    public class SimplicateHourItem
    {
        [JsonPropertyName("employee")]
        public SimplicateEmployee? Employee { get; set; }

        [JsonPropertyName("project")]
        public SimplicateProject? Project { get; set; }

        [JsonPropertyName("type")]
        public SimplicateHourType? Type { get; set; }

        [JsonPropertyName("tariff")]
        public decimal Tariff { get; set; }

        [JsonPropertyName("hours")]
        public double Hours { get; set; }

        [JsonIgnore] // Don't serialize calculated property by default
        public decimal Amount
        {
            get
            {
                // Defensive: if negative hours/tariff are expected, remove checks below
                var hours = Convert.ToDecimal(Hours); // Safe: double to decimal
                var tariff = Tariff;
                // If you need to check for negative values, add:
                // if (hours < 0 || tariff < 0) return 0m;

                var amount = hours * tariff;

                // If you want to round to 2 decimals for currency (bankers rounding):
                return amount.ToAmount();
            }
        }
    }

    public class SimplicateEmployee
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class SimplicateProject
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    public class SimplicateHourType
    {
        [JsonPropertyName("label")]
        public string Label { get; set; } = string.Empty;
    }