using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.Graph;

namespace active_directory_aspnetcore_webapp_openidconnect_v2.Models
{
    public class SelectMeetingTimeModel
    {
        public System.Collections.Generic.IEnumerable<Microsoft.Graph.MeetingTimeSuggestion> MeetingTimeSuggestions { get; set; }
    }
}