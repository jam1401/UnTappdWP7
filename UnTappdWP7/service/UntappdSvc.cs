using System;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using System.Collections.Generic;
using System.IO;

namespace WPUntappd.service
{
    public delegate void SvcResponseEventHandler(object sender, OpenReadCompletedEventArgs e);
    public enum BadgeSorts { all = 1, beer = 2, venue = 3, special = 4 };
    public enum SearchSort { count = 1, name = 2, none=3 };
    public enum BreweryType { all = 1, macro = 2, micro = 3, local = 4 };
    public enum Period { daily = 1, weekly = 2, monthly = 3 };
    public enum SocialCheckin { on = 1, off = 2 };
    public enum Rating { none = 0, one = 1, two = 2, three = 3, four = 4, five = 5 };

    /// <summary>
    /// Provides Raw access to the untappd service
    /// </summary>
    public class UntappdSvc
    {
        
        /// <summary>
        /// Base URI for Untappd service
        /// </summary>
        private const string uriBase = "http://api.untappd.com/v3";

        /// <summary>
        /// Authentication hash string username:hash of passwd
        /// </summary>
        private string _authStringHash;

        /// <summary>
        /// API Key for service
        /// </summary>
        private string _apiKey;

        /// <summary>
        /// Last response returned from the service
        /// </summary>
        private string _lastRawResponse;

        /// <summary>
        /// The last requested URI
        /// </summary>
        private string _lastMethod = null;

        /// <summary>
        /// The set of characters that are unreserved in RFC 2396 but are NOT unreserved in RFC 3986.
        /// </summary>
        private static readonly string[] UriRfc3986CharsToEscape = new[] { "!", "*", "'", "(", ")" };
        private static readonly char[] HexUpperChars =
                         new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };

        /// <summary>
        /// This event is called on a network response from the UnTappd service
        /// </summary>
        public event SvcResponseEventHandler SvcResponseHandler;

        /// <summary>
        /// Holds the last method called on the class or null if uninitialized
        /// </summary>
        public string LastMethod {
            get {return _lastMethod;}
        }


        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="apiKey">API Key for the service</param>
        /// <param name="username">The username for the account</param>
        /// <param name="password">The password for the account</param>
        public UntappdSvc(string apiKey, string username = null, string password = null)
        {
            _apiKey = apiKey;
            _authStringHash = GenAuthStringHash(username, password);

        }


        #region WishList
        /// <summary>
        /// Adds a beers to the authenticated users wishlist
        /// </summary>
        /// <param name="beerId">The BID for the beer that is wished</param>
        public void AddToMyWishList(string beerId)
        {
            if (string.IsNullOrEmpty(beerId))
                throw new ArgumentNullException("beerId", "A BeerId must be supplied to the AddToMyWishList Method");

            var requestArguments = new Dictionary<string, string>();
            requestArguments["bid"] = beerId;

            SendAsyncRequest("add_to_wish", requestArguments, true);
        }

        /// <summary>
        /// Removes a beer from the wish list of the authenticated user
        /// </summary>
        /// <param name="beerId">The BID of the beer that is un-wished</param>
        public void RemoveFromMyWishList(string beerId)
        {
            if (string.IsNullOrEmpty(beerId))
                throw new ArgumentNullException("beerId", "A BeerId must be supplied to the RemoveFromMyWishList Method");

            var requestArguments = new Dictionary<string, string>();
            requestArguments["bid"] = beerId;

            SendAsyncRequest("remove_from_wish", requestArguments, true);
        }
        #endregion


        #region FriendFeed
        /// <summary>
        ///  Returns the authenticated user's friend feed
        /// </summary>
        /// <param name="since">numeric ID of last checking</param>
        /// <param name="offset">offset within the data feed to move to</param>
        public void MyFriendFeed(string since = null, string offset = null)
        {
            var requestArguments = new Dictionary<string, string>();
            requestArguments["since"] = string.IsNullOrEmpty(since) ? "" : since;
            requestArguments["offset"] = string.IsNullOrEmpty(offset) ? "" : offset;

            SendAsyncRequest("feed", requestArguments, true);

        }

        /// <summary>
        /// Obtains the list of any pending friend requests
        /// </summary>
        public void MyPendingFriends()
        {
            var requestArguments = new Dictionary<string, string>();


            SendAsyncRequest("friend_pending", requestArguments, true);
        }

        /// <summary>
        /// Accepts a friend on behalf of the authenticated user
        /// </summary>
        /// <param name="requestingFriendId"> user id of the new friend</param>
        public void AcceptMyFriendRequest(string requestingFriendId)
        {
            if (string.IsNullOrEmpty(requestingFriendId))
                throw new ArgumentNullException("requestingFriendId", "A Friend Id must be supplied to the AcceptMyFriendRequest Method");

            var requestArguments = new Dictionary<string, string>();
            requestArguments["target_id"] = requestingFriendId;

            SendAsyncRequest("friend_accept", requestArguments, true);

        }

        /// <summary>
        /// Rejects a friend on behalf of the authenticated user
        /// </summary>
        /// <param name="rejectedFriendId">user id of the rejected friend</param>
        public void RejectMyFriendRequest(string rejectedFriendId)
        {
            if (string.IsNullOrEmpty(rejectedFriendId))
                throw new ArgumentNullException("rejectedFriendId", "A Friend Id must be supplied to the RejectMyFriendRequest Method");

            var requestArguments = new Dictionary<string, string>();
            requestArguments["target_id"] = rejectedFriendId;

            SendAsyncRequest("friend_reject", requestArguments, true);
        }

        /// <summary>
        /// Removes a friend from the authenticated user list
        /// </summary>
        /// <param name="friendId">The ID of the friend to be revoked</param>
        public void RemoveMyFriend(string friendId)
        {
            if (string.IsNullOrEmpty(friendId))
                throw new ArgumentNullException("friendId", "A Friend Id must be supplied to the RemoveMyFriend Method");

            var requestArguments = new Dictionary<string, string>();
            requestArguments["target_id"] = friendId;

            SendAsyncRequest("friend_revoke", requestArguments, true);

        }

        /// <summary>
        /// Makes a friend request on behalf of the logged in user
        /// </summary>
        /// <param name="userId">the ID of the potential friend</param>
        public void MakeMyFriendRequest(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                throw new ArgumentNullException("userId", "A Friend Id must be supplied to the MakeMyFriendRequest Method");

            var requestArguments = new Dictionary<string, string>();
            requestArguments["target_id"] = userId;

            SendAsyncRequest("friend_request", requestArguments, true);

        }

        #endregion
        /// <summary>
        /// Get's a users information
        /// </summary>
        /// <param name="username">username of the requested user, or null for authenticated user</param>
        public void UserInfo(string username = null)
        {
            var requestArguments = new Dictionary<string, string>();
            if(!string.IsNullOrEmpty(username))
                requestArguments["user"] = username;


            SendAsyncRequest("user", requestArguments, true);

        }

        /// <summary>
        ///  This method allows you the obtain all the friend check-in feed of the selected user. 
        ///  This includes only beer checkin-ins the selected user. By default it will return at max 25 records.
        /// </summary>
        /// <param name="username">The username that you wish to call the request upon. If you do not provide a username - the feed will return results from the authenticated user.</param>
        /// <param name="since">The numeric ID of the last recent check-in. This provided to you in the next_query attribute.</param>
        /// <param name="offset">The offset that you like the dataset to begin with. Each set returns 25 max records, so yo can use that paginate the feed.</param>
        public void UserFeed(string username = null, string since = null, string offset = null)
        {
            var requestArguments = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(username))
                requestArguments["user"] = username;
            if (!string.IsNullOrEmpty(since))
                requestArguments["since"] = since;
            if (!string.IsNullOrEmpty(offset))
                requestArguments["offset"] = offset;

            SendAsyncRequest("user_feed", requestArguments, true);
        }

        /// <summary>
        /// This method retrieves all of a user's distinct beers. Authenticated user's distinct beers if username is null
        /// </summary>
        /// <param name="username">The username that you wish to call the request upon. If a username is not supplied - the feed will return results from the authenticated user.</param>
        /// <param name="offset">The offset for the dataset to begin with. Each set returns 25 max records, so you can use that paginate the feed.</param>
        public void UserDistinctBeers(string username = null, string offset = null)
        {
            var requestArguments = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(username))
                requestArguments["user"] = username;
            if (!string.IsNullOrEmpty(offset))
                requestArguments["offset"] = offset;

            SendAsyncRequest("user_distinct", requestArguments, true);

        }

        /// <summary>
        /// This method will return the last 25 friends for a selected user. 
        /// If you want to obtain the authenticated user's information, you don't need to pass the "username" parameter.
        /// </summary>
        /// <param name="username">The username of the person who you wish to obtain the information. If you wish to call the authenticated user's information, do not include this parameter in your call</param>
        /// <param name="offset">The offset for the dataset to begin with. Each set returns 25 max records</param>
        public void UserFriends(string username = null, string offset = null)
        {
            var requestArguments = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(username))
                requestArguments["user"] = username;
            if (!string.IsNullOrEmpty(offset))
                requestArguments["offset"] = offset;

            SendAsyncRequest("friends", requestArguments, true);
        }

        /// <summary>
        /// This method will allow you to see all the user's wish listed beers
        /// </summary>
        /// <param name="username">The username that you wish to call the request upon. If you do not provide a username - the feed will return results from the authenticated user.</param>
        /// <param name="offset">The offset that you like the dataset to begin with. Each set returns 25 max records, so you can use that paginate the feed.</param>
        public void UserWishList(string username = null, string offset = null)
        {
            var requestArguments = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(username))
                requestArguments["user"] = username;
            if (!string.IsNullOrEmpty(offset))
                requestArguments["offset"] = offset;

            SendAsyncRequest("wish_list", requestArguments, true);

        }

        /// <summary>
        /// This method will return all the user's badges. If you want to obtain the authenticated user's information, 
        /// you don't need to pass the "user" query string.
        /// </summary>
        /// <param name="username">The username of the person who you wish to obtain the badge information. If you wish to call the authenticated user's information, do not include this parameter in your call.</param>
        /// <param name="sort">The kind of badge you want default is All.</param>
        public void UserBadge(string username = null, BadgeSorts sort = BadgeSorts.all)
        {
            var requestArguments = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(username))
                requestArguments["user"] = username;
            
            requestArguments["sort"] = sort.ToString();

            SendAsyncRequest("user_badge", requestArguments, true);
        }

        /// <summary>
        /// This method will allow you to see extended information about a beer.
        /// </summary>
        /// <param name="beerId"> The numeric beer ID of the beer you wish to look up.</param>
        public void BeerInfo(string beerId)
        {
            if (string.IsNullOrEmpty(beerId))
                throw new ArgumentNullException("beerId", "beerId parameter must be set and not empty");

            var requestArguments = new Dictionary<string, string>();
            requestArguments["bid"] = beerId;

            SendAsyncRequest("beer_info", requestArguments, true);

        }

        /// <summary>
        /// This method will allow you to see all to search the Untappd database of beers.
        /// </summary>
        /// <param name="searchStr">The search term that you want to search.</param>
        /// <param name="offset">The offset that you like the dataset to begin with. Each set returns 25 max records, so you can use that paginate the results.</param>
        /// <param name="sort">"count" or "name" (default) - This can let you choose if you want the results to be returned in Alphabetical order (name) or by checkin count (count). By default the search returns all values in Alphabetical order.</param>
        public void BeerSearch(string searchStr, string offset = null, SearchSort sort = SearchSort.none)
        {
            if (string.IsNullOrEmpty(searchStr))
                throw new ArgumentNullException("searchStr", "searchString parameter must be set and not empty");

            var requestArguments = new Dictionary<string, string>();
            requestArguments["q"] = searchStr;
            if(sort != SearchSort.none) {
                if (sort == SearchSort.name || sort == SearchSort.count)
                    requestArguments["sort"] = sort.ToString();
                else
                    throw new ArgumentException("sort", "sort must be either SearchSort.name or SearchSort.count or SearchSort.none");
            }

            if (!string.IsNullOrEmpty(offset))
                requestArguments["offset"] = offset;

            SendAsyncRequest("beer_search", requestArguments, true);
        }

        /// <summary>
        /// This method allows you the obtain a feed for a single beer for Untappd. This includes only beer checkin-ins non private users by an authenticated user. By default it will return at max 25 records.
        /// </summary>
        /// <param name="beerId">The beer ID that you want to display checkins</param>
        /// <param name="since">The numeric ID of the last recent check-in. This provided to you in the next_query attribute.</param>
        /// <param name="offset">The offset that you like the dataset to begin with. Each set returns 25 max records, so you can use that paginate the feed</param>
        public void BeerFeed(string beerId, string since = null, string offset = null)
        {
            if (string.IsNullOrEmpty(beerId))
                throw new ArgumentNullException("beerId", "beerId parameter must be set and not empty");

            var requestArguments = new Dictionary<string, string>();
            requestArguments["bid"] = beerId;

            if (!string.IsNullOrEmpty(since))
                requestArguments["since"] = since;
            if (!string.IsNullOrEmpty(offset))
                requestArguments["offset"] = offset;

            SendAsyncRequest("beer_checkins", requestArguments, true);
        }

        /// <summary>
        /// This method will allow you to see extended information about a venue.
        /// </summary>
        /// <param name="venueId">The numeric venue ID of the beer you wish to look up.</param>
        public void VenueInfo(string venueId)
        {
            if (string.IsNullOrEmpty(venueId))
                throw new ArgumentNullException("venueId", "venueId parameter must be set and not empty");

            var requestArguments = new Dictionary<string, string>();
            requestArguments["venue_id"] = venueId;

            SendAsyncRequest("venue_info", requestArguments, true);
        }

        /// <summary>
        /// This method allows you the obtain a feed for a single venue for Untappd. This includes only beer checkin-ins non private users by an authenticated user. By default it will return at max 25 records.
        /// </summary>
        /// <param name="venueId">The Venue ID that you want to display checkins</param>
        /// <param name="since">The numeric ID of the last recent check-in. This provided to you in the next_query attribute.</param>
        /// <param name="offset"> The offset that you like the dataset to begin with. Each set returns 25 max records, so you can use that paginate the feed.</param>
        public void VenueFeed(string venueId, string since = null, string offset = null)
        {
            if (string.IsNullOrEmpty(venueId))
                throw new ArgumentNullException("venueId", "venueId parameter must be set and not empty");

            var requestArguments = new Dictionary<string, string>();
            requestArguments["venue_id"] = venueId;

            if (!string.IsNullOrEmpty(since))
                requestArguments["since"] = since;
            if (!string.IsNullOrEmpty(offset))
                requestArguments["offset"] = offset;

            SendAsyncRequest("venue_checkins", requestArguments, true);
        }

        /// <summary>
        /// This method allows you the obtain a feed for a single brewery for Untappd. This includes only beer checkin-ins non private users by an authenticated user. By default it will return at max 25 records.
        /// </summary>
        /// <param name="breweryID">The Brewery ID that you want to display checkins</param>
        /// <param name="since">The numeric ID of the last recent check-in. This provided to you in the next_query attribute.</param>
        /// <param name="offset">The offset that you like the dataset to begin with. Each set returns 25 max records, so you can use that paginate the feed.</param>
        public void BreweryFeed(string breweryID, string since = null, string offset = null)
        {
            if (string.IsNullOrEmpty(breweryID))
                throw new ArgumentNullException("breweryID", "breweryID parameter must be set and not empty");

            var requestArguments = new Dictionary<string, string>();
            requestArguments["brewery_id"] = breweryID;
            if (!string.IsNullOrEmpty(since))
                requestArguments["since"] = since;
            if (!string.IsNullOrEmpty(offset))
                requestArguments["offset"] = offset;

            SendAsyncRequest("brewery_checkins", requestArguments, true);
        }

        /// <summary>
        /// This method will allow you to see extended information about a brewery.
        /// </summary>
        /// <param name="breweryId">The numeric brewery ID of the beer you wish to look up.</param>
        public void BreweryInfo(string breweryId)
        {
            if (string.IsNullOrEmpty(breweryId))
                throw new ArgumentNullException("breweryId", "breweryId parameter must be set and not empty");

            var requestArguments = new Dictionary<string, string>();
            requestArguments["brewery_id"] = breweryId;

            SendAsyncRequest("brewery_info", requestArguments, true);
        }

        /// <summary>
        /// This method will allow you to see all to search the Untappd database of breweries.
        /// </summary>
        /// <param name="searchString">The search term that you want to search.</param>
        public void BrewerySearch(string searchString)
        {
            if (string.IsNullOrEmpty(searchString))
                throw new ArgumentNullException("searchString", "searchString parameter must be set and not empty");
            var requestArguments = new Dictionary<string, string>();
            requestArguments["q"] = searchString;

            SendAsyncRequest("brewery_search", requestArguments, true);
        }

        /// <summary>
        /// This method allows you the obtain all the public feed for Untappd. This includes only beer checkin-ins non private users by an authenticated user. By default it will return at max 25 records.
        /// </summary>
        /// <param name="since">The numeric ID of the last recent check-in. This provided to you in the next_query attribute.</param>
        /// <param name="offset">The offset that you like the dataset to begin with. Each set returns 25 max records, so you can use that paginate the feed.</param>
        /// <param name="longitude">The numeric Longitude to filter the public feed</param>
        /// <param name="latitude"> The numeric Latitude to filter the public feed</param>
        /// <param name="radius">The numeric radius that you are trying to search within. The maximum for this value is 50 and the default is 5</param>
        public void PublicFeed(string since = null, string offset = null, string longitude = null, string latitude = null, string radius = null)
        {
            
            var requestArguments = new Dictionary<string, string>();

            if (!string.IsNullOrEmpty(since))
                requestArguments["since"] = since;
            if (!string.IsNullOrEmpty(offset))
                requestArguments["offset"] = offset; 
            if (IsGeo(longitude))
                requestArguments["geolng"] = longitude; 
            if (IsGeo(latitude))
                requestArguments["geolat"] = latitude;
            if (!string.IsNullOrEmpty(radius))
            {
                radius = ValidateRadius(radius);
                requestArguments["radius"] = radius;
            }

            SendAsyncRequest("thepub", requestArguments);
        }

        /// <summary>
        /// This method will allow you to see all the user's distinct beers.
        /// </summary>
        /// <param name="type">4 options: "all", "macro", "micro", "local". "All" is set to defaul</param>
        /// <param name="limit"> The number of records that you will return (max 10)</param>
        /// <param name="age">3 options: "daily", "weekly", "monthly".</param>
        /// <param name="latitude">The numeric Latitude to filter the trending beer. This is required for local "type".</param>
        /// <param name="longitude">The numeric Longitude to filter the trending beer. This is required for local "type".</param>
        /// <param name="radius">The numeric radius that you are trying to see trending beers. The maximum for this value is "50" and the default is "5"</param>
        public void PublicTrending(BreweryType type = BreweryType.all, int limit = 10, Period age = Period.daily, string latitude = null, string longitude = null, string radius = null)
        {
            

            if(type == BreweryType.local && (IsGeo(latitude) || IsGeo(longitude)))
                throw new ArgumentNullException("type", "Lat/Long must be specified when type is local");

            if (limit > 10 || limit < 1)
                limit = 10;

            radius = ValidateRadius(radius);

            var requestArguments = new Dictionary<string, string>();
            requestArguments["type"] = type.ToString();
            requestArguments["limit"] = limit.ToString();
            requestArguments["age"] = age.ToString();
            if (string.IsNullOrEmpty(longitude))
                requestArguments["geolng"] = longitude;
            if (string.IsNullOrEmpty(latitude))
                requestArguments["geolat"] = latitude;
            if (string.IsNullOrEmpty(radius))
                requestArguments["radius"] = radius;

            SendAsyncRequest("thepub", requestArguments);
        }

        /// <summary>
        /// This method will allow you to see extended details for a particular checkin, which includes location, comments and toasts.
        /// </summary>
        /// <param name="checkinId">The numeric ID of the check-in.</param>
        public void checkinInfo(string checkinId)
        {
            if (string.IsNullOrEmpty(checkinId))
                throw new ArgumentNullException("checkinId", "checkinId must be set");
            var requestArguments = new Dictionary<string, string>();
            requestArguments["id"] = checkinId;

            SendAsyncRequest("details", requestArguments, true);
        }

        /// <summary>
        /// This will allow you to perform a live checkin
        /// </summary>
        /// <param name="beerId">The numeric Beer ID you want to check into.</param>
        /// <param name="fourSquareId">The MD5 hash ID of the Venue you want to attach the beer checkin. This HAS TO BE the MD5 non-numeric hash from the foursquare v2. Older numeric </param>
        /// <param name="userLat"> The numeric Latitude of the user. This is required if you add a location</param>
        /// <param name="userLong">The numeric Longitude of the user. This is required if you add a location.</param>
        /// <param name="shout">The text you would like to include as a comment of the checkin. Max of 140 characters.</param>
        /// <param name="facebook">Default = "off", Pass "on" to post to facebook</param>
        /// <param name="twitter">Default = "off", Pass "on" to post to twitter</param>
        /// <param name="foursquare">Default = "off", Pass "on" to checkin on foursquare</param>
        /// <param name="gowalla">Default = "off", Pass "on" to checkin on gowalla</param>
        /// <param name="rating">The rating score you would like to add for the beer</param>
        public void checkin(string beerId, string fourSquareId = null, string userLat = null, string userLong = null,
            string shout = null, SocialCheckin facebook = SocialCheckin.off, SocialCheckin twitter = SocialCheckin.off, SocialCheckin foursquare = SocialCheckin.off, SocialCheckin gowalla = SocialCheckin.off, Rating rating = Rating.none)
        {
            if (string.IsNullOrEmpty(beerId))
                throw new ArgumentNullException("beerId", "A value must be specified for the beerId");

            if (!string.IsNullOrEmpty(fourSquareId) && (!IsGeo(userLat) || !IsGeo(userLong)))
                throw new ArgumentException("fourSquareId", "If a foursquare ID is presented you must present a valid lat long");

            if(!string.IsNullOrEmpty(shout) && shout.Length > 140)
                throw new ArgumentException("shout", "Shout can only be 140 characters in length");

             var requestArguments = new Dictionary<string, string>();

            // Get the checking time from the local timezone as an offset from GMT
            TimeSpan offset = DateTimeOffset.Now.Offset;
            
            requestArguments["gmt_offset"] = offset.Hours.ToString();
            requestArguments["bid"] = beerId;
            if(!string.IsNullOrEmpty(fourSquareId))
                requestArguments["foursquare_id"] = fourSquareId;
            if(IsGeo(userLat))
                requestArguments["user_lat"] = userLat;
            if(IsGeo(userLong))
                requestArguments["user_long"] = userLong;
            if (!string.IsNullOrEmpty(shout))
                requestArguments["shout"] = shout;
            requestArguments["facebook"] = facebook.ToString();
            requestArguments["twitter"] = twitter.ToString();
            requestArguments["foursquare"] = foursquare.ToString();
            requestArguments["gowalla"] = gowalla.ToString();
            if (rating != Rating.none)
                requestArguments["rating_value"] = rating.ToString();

            SendAsyncRequest("checkin", requestArguments, true);

        }

        /// <summary>
        /// This method will allow you comment on a checkin.
        /// </summary>
        /// <param name="checkinId">The numeric check-in ID that you wish to add a comment.</param>
        /// <param name="comment">The comment text that you would like to add. It must be less than 140 characters.</param>
        public void checkinComment(string checkinId, string comment)
        {
            if (string.IsNullOrEmpty(checkinId))
                throw new ArgumentNullException("checkinId", "A checkinId must be present for this method");

            if (string.IsNullOrEmpty(comment))
                throw new ArgumentNullException("comment", "you must provide a valid comment parameter");

            if (comment.Length > 140)
                throw new ArgumentException("comment", "your comments must be 140 or less characters long");


            var requestArguments = new Dictionary<string, string>();
            requestArguments["checkin_id"] = checkinId;
            requestArguments["comment"] = comment;

            SendAsyncRequest("add_comment", requestArguments, true);

        }

        /// <summary>
        /// This method will allow you to delete your comment on a checkin.
        /// </summary>
        /// <param name="commentId">The comment ID you wish to delete.</param>
        public void checkinRemoveComment(string commentId)
        {
            if (string.IsNullOrEmpty(commentId))
                throw new ArgumentNullException("commentId", "A commentId must be present for this method");

            var requestArguments = new Dictionary<string, string>();
            requestArguments["comment_id"] = commentId;

            SendAsyncRequest("delete_comment", requestArguments, true);
        }

        /// <summary>
        /// This method will allow you to toast a checkin.
        /// </summary>
        /// <param name="checkinId">The checkin ID you wish you toast.</param>
        public void checkinToast(string checkinId)
        {
            if (string.IsNullOrEmpty(checkinId))
                throw new ArgumentNullException("checkinId", "A checkinId must be present for this method");
            var requestArguments = new Dictionary<string, string>();
            requestArguments["checkin_id"] = checkinId;

            SendAsyncRequest("toast", requestArguments, true);
        }

        /// <summary>
        /// This method will allow you to delete your toast on a checkin.
        /// </summary>
        /// <param name="checkinId">The checkin ID you wish you remove your toast.</param>
        public void checkinRemoveToast(string checkinId)
        {
            if (string.IsNullOrEmpty(checkinId))
                throw new ArgumentNullException("checkinId", "A commentId must be present for this method");
            var requestArguments = new Dictionary<string, string>();
            requestArguments["checkin_id"] = checkinId;

            SendAsyncRequest("delete_toast", requestArguments, true);
        }

        #region HTTP
        private void SendAsyncRequest(string method, IDictionary<string, string> args, Boolean requireAuth = false)
        {
            _lastMethod = null;
            // Add the API key
            args.Add("key", _apiKey);
            WebClient untappdClient = new WebClient();
            untappdClient.OpenReadCompleted += new OpenReadCompletedEventHandler(untappdClient_OpenReadCompleted);

            // Set the auth header
            if (requireAuth)
            {
                System.Text.Encoding enc = System.Text.Encoding.UTF8;
                byte[] authByteArray = enc.GetBytes(_authStringHash);
                string basicAuth = "Basic " + System.Convert.ToBase64String(authByteArray);
                untappdClient.Headers[HttpRequestHeader.Authorization] = basicAuth;
            }
            Uri serviceUri = new Uri(uriBase + "/" + method + "?" + BuildQueryList(args), UriKind.Absolute);
            untappdClient.OpenReadAsync(serviceUri);
            _lastMethod = method;
        }
        #endregion

        #region QueryStringEncoding
        private string BuildQueryList(IEnumerable<KeyValuePair<string, string>> args)
        {
            var sb = new StringBuilder();
            foreach (var argument in args)
            {
                if (sb.Length > 0) sb.Append('&');
                sb.AppendFormat("{0}={1}", argument.Key, UrlEncode(argument.Value));
            }
            return sb.ToString();
        }

        private string UrlEncode(string value)
        {
            // Start with RFC 2396 escaping by calling the .NET method to do the work.
            // This MAY sometimes exhibit RFC 3986 behavior (according to the documentation).
            // If it does, the escaping we do that follows it will be a no-op since the
            // characters we search for to replace can't possibly exist in the string.
            var escaped = new StringBuilder(Uri.EscapeDataString(value));
            foreach (string t in UriRfc3986CharsToEscape)
            {
                escaped.Replace(t, HexEscape(t[0]));
            }
            return escaped.ToString();
        }

        private string HexEscape(char character)
        {
            var to = new char[3];
            int pos = 0;
            EscapeAsciiChar(character, to, ref pos);
            return new string(to);
        }

        private void EscapeAsciiChar(char ch, char[] to, ref int pos)
        {
            to[pos++] = '%';
            to[pos++] = HexUpperChars[(ch & 240) >> 4];
            to[pos++] = HexUpperChars[ch & '\x000f'];
        }

        private void untappdClient_OpenReadCompleted(object sender, OpenReadCompletedEventArgs e)
        {
            if (SvcResponseHandler != null)
            {
                SvcResponseHandler(this, e);
            }
            else
            {
                // Debug case just send to the Console
                if (e.Error == null)
                {
                    Console.WriteLine("Good Result");
                    StreamReader reader = new StreamReader(e.Result);
                    string str = reader.ReadLine();
                    while (str != null)
                    {
                        Console.WriteLine(str);
                        str = reader.ReadLine();
                    }
                }
                else
                {
                    Console.WriteLine("Error");
                    Console.WriteLine(e.Error.Message);
                    try
                    {
                        Console.WriteLine(e.Result);
                    }
                    catch (WebException ex)
                    {
                        Console.WriteLine("Exception from remote host HTTP Status is " + ex.Status.ToString());
                        WebResponse resp = ex.Response;

                        StreamReader reader = new StreamReader(resp.GetResponseStream());
                        string str = reader.ReadLine();
                        while (str != null)
                        {
                            Console.WriteLine(str);
                            str = reader.ReadLine();
                        }
                    }
                }

            }
        }
        #endregion

        #region Validation

        private string ValidateRadius(string rad)
        {
            int i = 0;
            if (rad == null)
                return "5";

            if (!int.TryParse(rad, out i))
                return "5";

            if (i > 50)
                return "50";
            if (i < 5)
                return "5";

            return rad;
        }
        /// <summary>
        /// Checks to see if a string is an number
        /// </summary>
        /// <param name="s">string to be testing</param>
        /// <returns></returns>
        private bool IsNumber(string s)
        {
            if (s == null)
                return false;
            int i = 0;
            return int.TryParse(s, out i);
        }

        /// <summary>
        /// Checks to see if a string is a geospacial parameter in decimal form
        /// </summary>
        /// <param name="s">string to be tested</param>
        /// <returns></returns>
        private bool IsGeo(string s)
        {
            if (s == null)
                return false;

            Regex pattern = new Regex("/^(\\-?\\d+(\\.\\d+)?),\\s*(\\-?\\d+(\\.\\d+)?)$/");

            return pattern.IsMatch(s);

        }
        #endregion

        #region AuthenticationHashing
        /// <summary>
        /// Creates the authentication string for access to the service
        /// </summary>
        /// <param name="username">The username for the account</param>
        /// <param name="password">The password for the account</param>
        /// <returns>The hashed authentication string</returns>
        private string GenAuthStringHash(string username, string password)
        {
            if (username != null && password != null)
            {
                return username + ":" + GetMD5Hash(password);
            }
            else
            {
                return null;
            }
        }


        /// <summary>
        /// Computes the MD5 hash of a string
        /// </summary>
        /// <param name="input">the string for which the hash is required</param>
        /// <returns> the hash value for the input string</returns>
        private string GetMD5Hash(string input)
        {
            byte[] bs = System.Text.Encoding.UTF8.GetBytes(input);
            MD5Managed md5 = new MD5Managed();
            byte[] hash = md5.ComputeHash(bs);

            StringBuilder sb = new StringBuilder();
            foreach (byte b in hash)
            {
                sb.Append(b.ToString("x2").ToLower());
            }

            return sb.ToString();
        }
        #endregion

    }


}
