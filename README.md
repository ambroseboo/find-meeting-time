# This is a project for the Microsoft Hack-Together Hackathon.

## This is a MVC (Model-View-Controller) web application built on .NET. 

This web application aims to remedy the pain point of scheduling meetings with colleagues. 
- Currently, using Outlook to schedule a meeting for a group of people is difficult. It is guesswork to put a time slot and see if there are any conflicts for others. 
- The suggestion tab in Outlook is not very useful; it shows a few recommended time slots, typically the earliest ones, or just the ones within this week or next week.

This application aims to take a list of emails (people) and a meeting duration and generate available meeting timeslots (where everyone is available) in the user-provided time period. 
- It does so by using Microsoft Graph API to query for everyone's calendars, and seeking a timeslot where everyone does not have an ongoing meeting.

### Additional Features
- Select meeting time and send meeting invite to all attendees.

[![Hack Together: Microsoft Graph and .NET](https://img.shields.io/badge/Microsoft%20-Hack--Together-orange?style=for-the-badge&logo=microsoft)](https://github.com/microsoft/hack-together)
