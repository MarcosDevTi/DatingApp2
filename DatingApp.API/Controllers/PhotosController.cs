using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using AutoMapper;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using DatingApp.API.Data;
using DatingApp.API.DTOs;
using DatingApp.API.Helpers;
using DatingApp.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace DatingApp.API.Controllers
{
    [Authorize]
    [Route("api/users/{userId}/photos")]
    [ApiController]
    public class PhotosController : ControllerBase
    {
        private readonly IDatingRepository _repository;
        private readonly IMapper _mapper;
        private readonly IOptions<CloudinarySettings> _cloudinaryConfig;
        private Cloudinary _cloudinary;
        public PhotosController(IDatingRepository repository, IMapper mapper, IOptions<CloudinarySettings> cloudinaryConfig)
        {
            _repository = repository;
            _mapper = mapper;
            _cloudinaryConfig = cloudinaryConfig;

            var acc = new Account(
           _cloudinaryConfig.Value.CloudName,
           _cloudinaryConfig.Value.ApiKey,
           _cloudinaryConfig.Value.ApiSecret
                );
            _cloudinary = new Cloudinary(acc);
        }

        [HttpGet("{id}", Name = "GetPhoto")]
        public async Task<IActionResult> GetPhoto(int id)
        {
            var photoFromRepository = await _repository.GetPhoto(id);

            var photo = _mapper.Map<PhotoForReturnDto>(photoFromRepository);

            return Ok(photo);
        }

        [HttpPost]
        public async Task<IActionResult> AddPhotoForUser(int userId, [FromForm]PhotoForCreationDto photoForCreationDto)
        {
            if(userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
            return Unauthorized();

            var userFromRepository = await _repository.GetUser(userId);

            var file = photoForCreationDto.File;

            var uploadResult = new ImageUploadResult();
            if(file.Length > 0)
            {
                using(var stream = file.OpenReadStream())
                {
                    var uploadParams = new ImageUploadParams
                    {
                        File = new FileDescription(file.Name, stream),
                        Transformation = new Transformation()
                            .Width(500).Height(500).Crop("fill").Gravity("face")
                    };
                    uploadResult = _cloudinary.Upload(uploadParams);
                }
            }

            photoForCreationDto.Url = uploadResult.Uri.ToString();
            photoForCreationDto.PublicId = uploadResult.PublicId;

            var photo = _mapper.Map<Photo>(photoForCreationDto);

            if(!userFromRepository.Photos.Any(u => u.IsMain)) 
                photo.IsMain = true;

            userFromRepository.Photos.Add(photo);

            if(await _repository.SaveAll())
            {
                var photoToReturn = _mapper.Map<PhotoForReturnDto>(photo);
                return CreatedAtRoute("GetPhoto", new {id = photo.Id}, photoToReturn);
            }
               
            return BadRequest("Could not add the photo");
        }
    
        [HttpPost("{id}/setMain")]
        public async Task<IActionResult> SetMainPhoto(int userId, int id)
        {
            if(userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
            return Unauthorized();

            var user = await _repository.GetUser(userId);
            
            if(!user.Photos.Any(p => p.Id == id)) return Unauthorized();
            
            var photoFromRepository = await _repository.GetPhoto(id);

            if(photoFromRepository.IsMain) return BadRequest("This is already the main photo");

            var currentMainPhoto = await _repository.GetMainPhotoForUser(userId);
            currentMainPhoto.IsMain = false;

            photoFromRepository.IsMain = true;

            if(await _repository.SaveAll()) return NoContent();

            return BadRequest("Could not set photo to main");
        } 

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeletePhoto(int userId, int id)
        {
            if(userId != int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value))
            return Unauthorized();

            var user = await _repository.GetUser(userId);
            
            if(!user.Photos.Any(p => p.Id == id)) return Unauthorized();
            
            var photoFromRepository = await _repository.GetPhoto(id);

            if(photoFromRepository.IsMain) return BadRequest("You cannot delete your main photo");

            if(photoFromRepository.PublicId != null)
            {
                var deleteParams = new DeletionParams(photoFromRepository.PublicId);
            
                var result = _cloudinary.Destroy(deleteParams);

                if(result.Result == "ok") {
                    _repository.Delete(photoFromRepository);
                }
            }
            
            if(photoFromRepository.PublicId == null)
            {
                _repository.Delete(photoFromRepository);
            }

            if(await _repository.SaveAll()) return Ok();
            return BadRequest("Failed to delete the photo");
        }
    }
}