using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ShieldHrm;
using static ShieldHrm.EmployeeServiceClient;

namespace FrontEnd.Controllers
{
    [Produces("application/json")]
    [Route("api/[controller]")]
    public class TeamsController : Controller
    {
        private readonly HttpClient httpClient;

        public TeamsController(HttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        // GET: api/Teams
        [HttpGet("")]
        public async Task<IActionResult> Get()
        {
            List<Team> result = new List<Team>();
            string proxyUrl = GetProxyUrl();

            using (HttpResponseMessage response = await httpClient.GetAsync(proxyUrl))
            {
                response.EnsureSuccessStatusCode();
                result.AddRange(JsonConvert.DeserializeObject<List<Team>>(await response.Content.ReadAsStringAsync()));
            }

            return Json(result);
        }

        // PUT: api/Teams/name
        [HttpPut("{name}")]
        public async Task<IActionResult> Put(string name, [FromBody]string[] members)
        {
            string proxyUrl = GetProxyUrl(name);

            PowerGrid powerGrid = await CalculatePowerGridAsync(members);

            StringContent putContent = new StringContent(
                JsonConvert.SerializeObject(new Team
                {
                    Name = name,
                    Members = members,
                    PowerGrid = powerGrid,
                    Score = powerGrid.CalculateAverageScore()
                }),
                Encoding.UTF8,
                "application/json");

            putContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            using (HttpResponseMessage response = await httpClient.PutAsync(proxyUrl, putContent))
            {
                return new ContentResult()
                {
                    StatusCode = (int)response.StatusCode,
                    Content = await response.Content.ReadAsStringAsync()
                };
            }
        }

        // DELETE: api/Teams/name
        [HttpDelete("{name}")]
        public async Task<IActionResult> Delete(string name)
        {
            string proxyUrl = GetProxyUrl(name);

            using (HttpResponseMessage response = await httpClient.DeleteAsync(proxyUrl))
            {
                if (response.StatusCode != System.Net.HttpStatusCode.OK)
                {
                    return StatusCode((int)response.StatusCode);
                }
            }

            return Ok();
        }

        private static async Task<PowerGrid> CalculatePowerGridAsync(string[] members)
        {
            int intelligence = 0;
            int strength = 0;
            int speed = 0;
            int durability = 0;
            int energyProjection = 0;
            int fightingSkills = 0;

            EmployeeServiceClient client = new EmployeeServiceClient(EndpointConfiguration.BasicHttpBinding_IEmployeeService, "http://51.136.9.144:83/EmployeeService.svc/EmployeeService");

            try
            {
                foreach (string member in members)
                {
                    Employee employeeDetails = await client.GetEmployeeDetailsAsync(member);

                    intelligence += employeeDetails.Intelligence;
                    strength += employeeDetails.Strength;
                    speed += employeeDetails.Speed;
                    durability += employeeDetails.Durability;
                    energyProjection += employeeDetails.EnergyProjection;
                    fightingSkills += employeeDetails.FightingSkills;
                }
            }
            finally
            {
                await client.CloseAsync();
            }

            int teamSize = members.Length;

            return new PowerGrid
            {
                Intelligence = intelligence / teamSize,
                Strength = strength / teamSize,
                Speed = speed / teamSize,
                Durability = durability / teamSize,
                EnergyProjection = energyProjection / teamSize,
                FightingSkills = fightingSkills / teamSize
            };
        }

        private static string GetProxyUrl()
        {
            return $"http://localhost:5001/api/Teams";
        }

        private static string GetProxyUrl(string teamName)
        {
            return $"http://localhost:5001/api/Teams/{teamName}";
        }
    }

    public class Team
    {
        public string Name { get; set; }

        public string[] Members { get; set; }

        public int Score { get; set; }

        public PowerGrid PowerGrid { get; set; }
    }

    public class PowerGrid
    {
        public int Intelligence { get; set; }

        public int Strength { get; set; }

        public int Speed { get; set; }

        public int Durability { get; set; }

        public int EnergyProjection { get; set; }

        public int FightingSkills { get; set; }

        public int CalculateAverageScore()
        {
            return (Intelligence + Strength + Speed + Durability + EnergyProjection + FightingSkills) / 6;
        }
    }
}