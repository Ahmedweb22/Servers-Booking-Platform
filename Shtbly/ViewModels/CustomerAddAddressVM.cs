using System.ComponentModel.DataAnnotations;

namespace Shtbly.ViewModels
{
    public class CustomerAddAddressVM
    {
        [Required(ErrorMessage = "CityRequired")]
        [MaxLength(100, ErrorMessage = "CityMaxLength")]
        public string City { get; set; } = string.Empty;

        [MaxLength(100, ErrorMessage = "DistrictMaxLength")]
        public string? District { get; set; }

        [Required(ErrorMessage = "StreetRequired")]
        [MaxLength(255, ErrorMessage = "StreetMaxLength")]
        public string Street { get; set; } = string.Empty;

        [Range(-90.0, 90.0, ErrorMessage = "InvalidLatitude")]
        public double? Lat { get; set; }

        [Range(-180.0, 180.0, ErrorMessage = "InvalidLongitude")]
        public double? Lng { get; set; }

        public bool IsDefault { get; set; }
    }
}
