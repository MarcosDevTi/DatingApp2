using System.ComponentModel.DataAnnotations;

namespace DatingApp.API.DTOs
{
    public class UserForRegisterDto
    {
        public string Username { get; set; }
         [StringLength(30, MinimumLength = 2, ErrorMessage ="The password must been between 2 and 30 characteres")]
        public string Password { get; set; }
    }
}