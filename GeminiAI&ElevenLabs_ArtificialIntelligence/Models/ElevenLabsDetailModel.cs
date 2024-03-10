using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GeminiAI_ElevenLabs_ArtificialIntelligence.Models
{
    public class ElevenLabsDetailModel
    {
        public string status { get; set; }
        public string message { get; set; }
        public int character_used { get; set; }
        public int character_limit { get; set; }
    }
}
