using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace SmartXGateway.Models
{
    public class SensorRegistrationViewModel
    {
        [Required, Display(Name = "MAC Address")]
        [RegularExpression(@"^([0-9A-Fa-f]{2}:){5}[0-9A-Fa-f]{2}$",
            ErrorMessage = "MAC must be in format AA:BB:CC:DD:EE:FF")]
        public string MacAddress { get; set; } = string.Empty;

        [Required, Display(Name = "Deployment Location")]
        public string Location { get; set; } = string.Empty;

        [Required]
        public SensorCategory Category { get; set; }

        [Display(Name = "Attach config / photo / log (optional)")]
        public IFormFile? Attachment { get; set; }
    }
}

/* ============================================================
   REFERENCES
   ------------------------------------------------------------
   Wentworth, P., 2015. Think Sharply with C#. [Online] Available 
       at: http://www.ict.ru.ac.za/Resources/ThinkSharply/ThinkSharply/index.html [Accessed 18 November 2025].
   ============================================================ */