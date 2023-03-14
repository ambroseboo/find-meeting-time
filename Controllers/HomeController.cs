using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;
using Microsoft.Graph;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using active_directory_aspnetcore_webapp_openidconnect_v2.Models;
using System;
using System.Linq;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Collections.Generic;
using System.ComponentModel;

namespace active_directory_aspnetcore_webapp_openidconnect_v2.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private readonly Microsoft.Graph.GraphServiceClient _graphServiceClient;

        // Values from app registration
        private static readonly string tenant_id = "dc6d3122-6e4f-4b41-817a-023f598a7169";
        private static readonly string client_id = "b8f4001d-83a5-40c5-9d74-706d5c26aa25";
        private static readonly string client_secret = "tXe8Q~R9yfr68iq25zhLFwLpBUQhkiA1V-CvBb-7";

        // Get bearer token
        private readonly string baseAddress = $"https://login.microsoftonline.com/{tenant_id}/oauth2/v2.0/token";
        private static readonly string grant_type = "client_credentials";
        private static readonly string[] scopes = new string[]{"https://graph.microsoft.com/.default"};

        private readonly Dictionary<string, string> form = new Dictionary<string, string> {
            {"grant_type", grant_type},
            {"client_id", client_id},
            {"client_secret", client_secret},
            {"scope", scopes[0]}
        };

        public HomeController(ILogger<HomeController> logger,
                          Microsoft.Graph.GraphServiceClient graphServiceClient) {
             _logger = logger;
            _graphServiceClient = graphServiceClient;
       }

        [AuthorizeForScopes(ScopeKeySection = "DownstreamApi:Scopes")]
        public async Task<IActionResult> Index() {
            var user = await _graphServiceClient.Me.Request().GetAsync();
            ViewData["ApiResult"] = user.DisplayName;
            TempData["UserPrincipalName"] = user.UserPrincipalName;

            return View();
        }

        public IActionResult FindMeetingTime() {
            return View();
        }

        [HttpPost("FindMeetingTime")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> FindMeetingTime(string subject, string body, DateTime startDateTime, DateTime endDateTime, string attendees, string location, int minutes) {
            // create attendeeBase list
            string[] attendeeList = attendees.Split(',');
            System.Collections.Generic.List<AttendeeBase> attendeeBaseList = new System.Collections.Generic.List<AttendeeBase>();
            HttpClient httpClient = await createHttpClientWithAuth();

            foreach (var attendeeEmail in attendeeList) {
                // get display name
                var path = $"https://graph.microsoft.com/v1.0/users/{attendeeEmail}?$select=displayName";
                HttpResponseMessage graphResponse = await httpClient.GetAsync(path);

                attendeeBaseList.Add(
                    new AttendeeBase {
                        EmailAddress = new EmailAddress {
                            Address = attendeeEmail,
                            Name = await graphResponse.Content.ReadAsStringAsync()
                        },
                        Type = AttendeeType.Required,
                    }
                );
            }

            // convert meeting duration to TimeSpan
            var meetingHours = minutes / 60;
            var meetingMinutes = minutes % 60;
            var meetingDuration = new Duration(new TimeSpan(meetingHours, meetingMinutes, 0));

            var timeConstraint = new TimeConstraint {
                TimeSlots = new System.Collections.Generic.List<TimeSlot> {
                    new TimeSlot {
                        Start = new DateTimeTimeZone {
                            DateTime = startDateTime.ToString("s"),
                            TimeZone = "Singapore Standard Time",
                        },
                        End = new DateTimeTimeZone{
                            DateTime = endDateTime.ToString("s"),
                            TimeZone = "Singapore Standard Time",
                        },
                    },
                },
            };

            var locationConstraint = new LocationConstraint {
                IsRequired = false,
                SuggestLocation = false,
                Locations = new System.Collections.Generic.List<LocationConstraintItem> {
                    new LocationConstraintItem {
                        ResolveAvailability = false,
                        DisplayName = location,
                    },
                },
            };

            var maxCandidates = 15;
        
            // call graph API
            var result = await _graphServiceClient.Me
                .FindMeetingTimes(attendeeBaseList, locationConstraint, timeConstraint, meetingDuration, maxCandidates)
                .Request()
                .PostAsync();


            if (!String.IsNullOrEmpty(result.EmptySuggestionsReason)) {
                Console.WriteLine(result.EmptySuggestionsReason);
                return RedirectToAction("NoMeetingTime");
            }

            TempData["meetingTimeSuggestions"] = JsonConvert.SerializeObject(result.MeetingTimeSuggestions);
            TempData["attendees"] = JsonConvert.SerializeObject(attendeeBaseList);
            TempData["attendeesString"] = attendees;
            TempData["subject"] = subject;
            TempData["body"] = body;
            TempData["location"] = location;

            return RedirectToAction("SelectMeetingTime");
        }

        public IActionResult SelectMeetingTime() {
            return View(new SelectMeetingTimeModel { MeetingTimeSuggestions = JsonConvert.DeserializeObject<System.Collections.Generic.IEnumerable<Microsoft.Graph.MeetingTimeSuggestion>>((string)TempData["meetingTimeSuggestions"]) });
        }

        public async Task<IActionResult> Select(string id, string id2) {
            TempData["start"] = id.Replace("%2F", "/");
            TempData["end"] = id2.Replace("%2F", "/");

            // get attendees as list of Attendee
            var attendees = JsonConvert.DeserializeObject<System.Collections.Generic.List<AttendeeBase>>((string)TempData["attendees"]);
            System.Collections.Generic.List<Attendee> attendeeList = new System.Collections.Generic.List<Attendee>();
            foreach (var attendeeBase in attendees) {
                attendeeList.Add(
                    new Attendee {
                        EmailAddress = attendeeBase.EmailAddress,
                        Type = AttendeeType.Required
                    }
                );
            }

            // create request body for post request
            var requestBody = new Event {
                Subject = (string)TempData["subject"],
                Body = new ItemBody {
                    ContentType = BodyType.Html,
                    Content = (string)TempData["body"],
                },
                Start = new DateTimeTimeZone {
                    DateTime = (string)TempData["start"],
                    TimeZone = "Singapore Standard Time",
                },
                End = new DateTimeTimeZone {
                    DateTime = (string)TempData["end"],
                    TimeZone = "Singapore Standard Time",
                },
                Location = new Location {
                    DisplayName = (string)TempData["location"],
                },
                Attendees = attendeeList,
                AllowNewTimeProposals = false,
            };

            string json = JsonConvert.SerializeObject(requestBody);
            StringContent httpContent = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

            var path = $"https://graph.microsoft.com/v1.0/users/{TempData["UserPrincipalName"]}/calendar/events";
            HttpClient httpClient = await createHttpClientWithAuth();
            HttpResponseMessage graphResponse = await httpClient.PostAsync(path, httpContent);
            return View();
        }


        public IActionResult NoMeetingTime() {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }

        private async Task<HttpClient> createHttpClientWithAuth() {
            var httpClient = new HttpClient();
            HttpResponseMessage tokenResponse = await httpClient.PostAsync(baseAddress, new FormUrlEncodedContent(form));
            var jsonContent = await tokenResponse.Content.ReadAsStringAsync();
            Token token = JsonConvert.DeserializeObject<Token>(jsonContent);
            var authTokenStr = token.AccessToken;

            if (!tokenResponse.IsSuccessStatusCode) {
                throw new HttpRequestException("Call to get Token with HttpClient failed.");
            }

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authTokenStr);
            return httpClient;
        }
    }

    internal class Token
    {
        [JsonProperty("access_token")]
        public string AccessToken { get; set; }

        [JsonProperty("token_type")]
        public string TokenType { get; set; }

        [JsonProperty("expires_in")]
        public int ExpiresIn { get; set; }

        [JsonProperty("refresh_token")]
        public string RefreshToken { get; set; }
    }
}
