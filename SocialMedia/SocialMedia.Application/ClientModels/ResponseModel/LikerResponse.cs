using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SocialMedia.Application.ClientModels.ResponseModel
{
    public class LikerResponse
    {
        public long UserId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public DateTime LikedAt { get; set; }
    }
}
