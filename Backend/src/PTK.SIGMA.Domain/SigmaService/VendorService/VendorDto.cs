using System.ComponentModel.DataAnnotations;

namespace PTK.SIGMA.Domain.SigmaService
{
    public class VendorResponseDto
    {
        public int Id { get; set; }
        public string NamaVendor { get; set; } = string.Empty;
    }

    public class VendorCreateDto
    {
        [Required(ErrorMessage = "Nama vendor wajib diisi!")]
        public string NamaVendor { get; set; } = string.Empty;
    }
}
