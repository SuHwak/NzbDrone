﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using NzbDrone.Core.Repository;
using NzbDrone.Web.Helpers.Validation;

namespace NzbDrone.Web.Models
{
    public class IndexerSettingsModel
    {
        [DataType(DataType.Text)]
        [DisplayName("User ID")]
        [Description("User ID for NZBsRus")]
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        [RequiredIf("NzbsRUsEnabled", true, ErrorMessage = "User ID Required when NzbsRus is enabled")]
        public String NzbsrusUId { get; set; }

        [DataType(DataType.Text)]
        [DisplayName("API Key")]
        [Description("API Key for NZBsRus")]
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        [RequiredIf("NzbsRUsEnabled", true, ErrorMessage = "API Key Required when NzbsRus is enabled")]
        public String NzbsrusHash { get; set; }

        [DataType(DataType.Text)]
        [DisplayName("UID")]
        [Description("UserID for File Sharing Talk")]
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        [RequiredIf("FileSharingTalkEnabled", true, ErrorMessage = "UserID Required when File Sharing Talk is enabled")]
        public String FileSharingTalkUid { get; set; }

        [DataType(DataType.Text)]
        [DisplayName("Secret")]
        [Description("Password Secret for File Sharing Talk")]
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        [RequiredIf("FileSharingTalkEnabled", true, ErrorMessage = "Password Secret Required when File Sharing Talk is enabled")]
        public String FileSharingTalkSecret { get; set; }

        [DataType(DataType.Text)]
        [DisplayName("Username")]
        [Description("Username for omgwtfnzbs")]
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        [RequiredIf("OmgwtfnzbsEnabled", true, ErrorMessage = "Username is required when omgwtfnzbs is enabled")]
        public String OmgwtfnzbsUsername { get; set; }

        [DataType(DataType.Text)]
        [DisplayName("API Key")]
        [Description("API Key for omgwtfnzbs")]
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        [RequiredIf("OmgwtfnzbsEnabled", true, ErrorMessage = "API Key is required when omgwtfnzbs is enabled")]
        public String OmgwtfnzbsApiKey { get; set; }

        [DisplayName("NZBsRUs")]
        [Description("Enable downloading episodes from NZBsRus")]
        public bool NzbsRUsEnabled { get; set; }

        [DisplayName("Newznab")]
        [Description("Enable downloading episodes from Newznab Providers")]
        public bool NewznabEnabled { get; set; }

        [DisplayName("Womble's Index")]
        [Description("Enable downloading episodes from Womble's Index")]
        public bool WomblesEnabled { get; set; }

        [DisplayName("File Sharing Talk")]
        [Description("Enable downloading episodes from File Sharing Talk")]
        public bool FileSharingTalkEnabled { get; set; }

        [DisplayName("NzbIndex")]
        [Description("Enable downloading episodes from NzbIndex")]
        public bool NzbIndexEnabled { get; set; }

        [DisplayName("NzbClub")]
        [Description("Enable downloading episodes from NzbClub")]
        public bool NzbClubEnabled { get; set; }

        [DisplayName("omgwtfnzbs")]
        [Description("Enable downloading episodes from omgwtfnzbs")]
        public bool OmgwtfnzbsEnabled { get; set; }

        [DisplayName("nzbx")]
        [Description("Enable downloading episodes from nzbx")]
        public bool NzbxEnabled { get; set; }

        [Required(ErrorMessage = "Please enter a valid number of days")]
        [DataType(DataType.Text)]
        [DisplayName("Retention")]
        [Description("Usenet provider retention in days (0 = unlimited)")]
        public int Retention { get; set; }

        [DisplayName("RSS Sync Interval")]
        [Description("Check for new episodes every X minutes")]
        [DisplayFormat(ConvertEmptyStringToNull = false)]
        [Required(AllowEmptyStrings = false, ErrorMessage = "You must enter a valid time in minutes")]
        [Range(15, 240, ErrorMessage = "Interval must be between 15 and 240 minutes")]
        public int RssSyncInterval { get; set; }

        public List<NewznabDefinition> NewznabDefinitions { get; set; }
    }
}