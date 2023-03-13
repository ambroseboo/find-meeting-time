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

namespace active_directory_aspnetcore_webapp_openidconnect_v2.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        private readonly GraphServiceClient _graphServiceClient;

        public HomeController(ILogger<HomeController> logger,
                          GraphServiceClient graphServiceClient) {
             _logger = logger;
            _graphServiceClient = graphServiceClient;
       }

        [AuthorizeForScopes(ScopeKeySection = "DownstreamApi:Scopes")]
        public async Task<IActionResult> Index() {
            var user = await _graphServiceClient.Me.Request().GetAsync();
            ViewData["ApiResult"] = user.DisplayName;

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
            foreach (var attendeeEmail in attendeeList) {
                // var attendeeUser = await _graphServiceClient.Users["{attendeeEmail}"].GetAsync();
                // var attendeeName = attendeeUser["displayName"];
                attendeeBaseList.Add(
                    new AttendeeBase {
                        EmailAddress = new EmailAddress {
                            Address = attendeeEmail,
                            Name = "attendeeName",
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
                            TimeZone = "Pacific Standard Time",
                        },
                        End = new DateTimeTimeZone{
                            DateTime = endDateTime.ToString("s"),
                            TimeZone = "Pacific Standard Time",
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
                return RedirectToAction("NoMeetingTime");
            }

            TempData["meetingTimeSuggestions"] = JsonConvert.SerializeObject(result.MeetingTimeSuggestions);
            TempData["attendees"] = JsonConvert.SerializeObject(attendeeBaseList);
            TempData["subject"] = subject;
            TempData["body"] = body;
            TempData["location"] = location;

            return RedirectToAction("SelectMeetingTime");
        }

        public IActionResult SelectMeetingTime() {
            return View(new SelectMeetingTimeModel { MeetingTimeSuggestions = JsonConvert.DeserializeObject<System.Collections.Generic.IEnumerable<Microsoft.Graph.MeetingTimeSuggestion>>((string)TempData["meetingTimeSuggestions"]) });
        }

        public IActionResult NoMeetingTime() {
            return View();
        }

        [AllowAnonymous]
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
