using System;
using System.IO;
using System.Threading.Tasks;
using AliceWritersNotepad.Models.Alice;
using AliceWritersNotepad.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace AliceWritersNotepad.Controllers
{
    [ApiController]
    [Route("/")]
    public class AliceController : ControllerBase
    {
        private readonly AliceService _aliceService;

        public AliceController(AliceService aliceService)
        {
            _aliceService = aliceService;
        }
        
        [HttpGet]
        public string Get()
        {
            return "It works!";
        }

        [HttpPost]
        public Task Post()
        {
            using var reader = new StreamReader(Request.Body);
            var body = reader.ReadToEnd();

            var request = JsonConvert.DeserializeObject<AliceRequest>(body, Utils.ConverterSettings);
            if (request == null)
            {
                Console.WriteLine("Request is null:");
                Console.WriteLine(body);
                return Response.WriteAsync("Request is null");
            }
            
            if (request.IsPing())
            {
                var pong = new AliceResponse(request).ToPong();
                var pongResponse = JsonConvert.SerializeObject(pong, Utils.ConverterSettings);
                return Response.WriteAsync(pongResponse);
            }

            Console.WriteLine($"REQUEST:\n{JsonConvert.SerializeObject(request, Utils.ConverterSettings)}\n");
            
            var response = _aliceService.HandleRequest(request);
            var stringResponse = JsonConvert.SerializeObject(response, Utils.ConverterSettings);

            Console.WriteLine($"RESPONSE:\n{stringResponse}\n");
            
            return Response.WriteAsync(stringResponse);
        }
    }
}