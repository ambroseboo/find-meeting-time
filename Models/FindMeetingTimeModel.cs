using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace active_directory_aspnetcore_webapp_openidconnect_v2.Models
{
    public class FindMeetingTimeModel
    {
        [Display(Name = "Subject/Title of Meeting")]
        [DataType(DataType.Text)]
        public string Subject { get; set; }

        [Display(Name = "Body Text")]
        [DataType(DataType.Text)]
        public string Body { get; set; }

        [Display(Name = "Start Date and Time")]
        [DataType(DataType.DateTime)]
        public DateTime StartDateTime { get; set; }

        [Display(Name = "End Date and Time")]
        [DataType(DataType.DateTime)]
        public DateTime EndDateTime { get; set; }

        [Display(Name = "Attendees (seperated by commas)")]
        [DataType(DataType.Text)]
        public string Attendees { get; set; }

        [Display(Name = "Location")]
        [DataType(DataType.Text)]
        public string Location { get; set; }

        [Display(Name = "Duration of meeting (in minutes)")]
        [RegularExpression("([1-9][0-9]*)", ErrorMessage = "Count must be a natural number")]
        public int Minutes { get; set; }
    }
}
