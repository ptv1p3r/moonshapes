using System;
using System.ComponentModel.DataAnnotations;
using System.Web;
using System.Web.Mvc;

namespace Moonshapes.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        [DataType(DataType.EmailAddress)]
        [Remote("doesEmailExist", "Users", AdditionalFields = "Id", HttpMethod = "POST", ErrorMessage = "Email já registado. indique um novo endereço de email.")]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Date)]
        [DisplayFormat(ApplyFormatInEditMode = true, DataFormatString = "{0:yyyy-MM-dd}")]
        public DateTime DataNascimento { get; set; }

        public string Foto { get; set; }

        public int Ordem { get; set; }

        [Required]
        public HttpPostedFileBase File { get; set; }
    }
}