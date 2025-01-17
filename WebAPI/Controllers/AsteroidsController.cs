﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Planets.WebAPI.Model;

namespace Planets.WebAPI.Controllers
{
    [ApiController]
    public class AsteroidsController : ControllerBase
    {
        private string baseAddress = "https://api.nasa.gov/neo/rest/v1/feed";

        public AsteroidsController()
        {
        }

        // GET api/asteroids/}
        [HttpGet]
        [Route("api/asteroids/test")]
        public bool Test()
        {
            return true;
        }

        // POST => GET api/asteroids/}
        [HttpPost]
        [Route("api/asteroids/")]
        public async Task<JsonResult> Get([FromBody] JToken jsonbody)
        {
            List<Asteroid> asteroids = new List<Asteroid>();

            SearchParams parameter = JsonConvert.DeserializeObject<SearchParams>(jsonbody.ToString());

            if (parameter.planet == "")
                return new JsonResult(new { Message = "You must specifie a target planet.", asteroids });

            try
            {
                //get from NASA api here

                HttpClient client = new HttpClient();

                client.BaseAddress = new Uri(baseAddress);

                var response = client.GetAsync("?start_date=" + parameter.start_date.ToString("yyyy-MM-dd") + "&end_date=" + parameter.end_date.ToString("yyyy-MM-dd") + "&api_key=" + parameter.api_key).Result;

                if (response.IsSuccessStatusCode)
                {
                    using (HttpContent content = response.Content)
                    {
                        string data = await content.ReadAsStringAsync();

                        try
                        {
                            var nasaNearEarthAsteroids = JsonConvert.DeserializeObject<NasaNearEarthAsteroids>(data);

                            if (nasaNearEarthAsteroids != null && nasaNearEarthAsteroids.near_earth_objects != null && nasaNearEarthAsteroids.near_earth_objects.Any())
                            {
                                var riskOfCollition = nasaNearEarthAsteroids.near_earth_objects.Where(a => a.is_potentially_hazardous_asteroid);

                                foreach (var asteroid in riskOfCollition)
                                {
                                    var newAsteroid = new Asteroid()
                                    {
                                        Name = asteroid.name,
                                        Planet = asteroid.close_approach_data.orbiting_body,
                                        Date = asteroid.close_approach_data.close_approach_date,
                                        Diameter = (asteroid.estimated_diameter.kilometers.estimated_diameter_max - asteroid.estimated_diameter.kilometers.estimated_diameter_min) == 0 ? asteroid.estimated_diameter.kilometers.estimated_diameter_max : (asteroid.estimated_diameter.kilometers.estimated_diameter_min + ((asteroid.estimated_diameter.kilometers.estimated_diameter_max - asteroid.estimated_diameter.kilometers.estimated_diameter_min) / 2)),
                                        Velocity = asteroid.close_approach_data.relative_velocity.kilometers_per_hour
                                    };

                                    asteroids.Add(newAsteroid);
                                }
                            }
                            else
                                return new JsonResult(new { Message = "No asteroids were found matching the search criteria. Please tsry a different combination of parameters.", asteroids });
                        }
                        catch
                        {
                            throw;
                        }
                    }
                }
            }
            catch (Exception)
            {
                var result = new JsonResult(new { Message = "The server failed to process this file. Please verify source data is compatible.", asteroids });

                result.StatusCode = 500;
            }

            return new JsonResult(new { Message = "", asteroids });
        }
    }
}
