using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Web.Http;

namespace VotingService.Controllers
{
    public class VotesController : ApiController
    {
        #region fields

        // Holds the votes and counts. NOTE: THIS IS NOT THREAD SAFE FOR THE PURPOSES OF THE LAB.
        static readonly Dictionary<string, int> _counts = new Dictionary<string, int>();

        // Used for health checks.
        public static long RequestCount;

        #endregion

        #region methods

        [HttpDelete]
        [Route("api/{key}")]
        public HttpResponseMessage Delete(string key)
        {
            var activityId = Guid.NewGuid().ToString();
            ServiceEventSource.Current.ServiceRequestStart("VotesController.Delete", activityId);

            Interlocked.Increment(ref RequestCount);

            if (!_counts.ContainsKey(key))
                return Request.CreateResponse(HttpStatusCode.NotFound);

            ServiceEventSource.Current.ServiceRequestStop("VotesController.Detete", activityId);

            return Request.CreateResponse(
                _counts.Remove(key) 
                    ? HttpStatusCode.OK 
                    : HttpStatusCode.NotFound);
        }

        // GET api/votes 
        [HttpGet]
        [Route("api/votes")]
        public HttpResponseMessage Get()
        {
            var activityId = Guid.NewGuid().ToString();
            ServiceEventSource.Current.ServiceRequestStart("VotesController.Get", activityId);

            Interlocked.Increment(ref RequestCount);

            var votes = new List<KeyValuePair<string, int>>(_counts.Count);
            foreach (var kvp in _counts)
            {
                votes.Add(kvp);
            }

            var response = Request.CreateResponse(HttpStatusCode.OK, votes);
            response.Headers.CacheControl = new CacheControlHeaderValue {NoCache = true, MustRevalidate = true};

            ServiceEventSource.Current.ServiceRequestStop("VotesController.Get", activityId);

            return response;
        }

        [HttpGet]
        [Route("api/{file}")]
        public HttpResponseMessage GetFile(string file)
        {
            var activityId = Guid.NewGuid().ToString();
            ServiceEventSource.Current.ServiceRequestStart("VotesController.GetFile", activityId);

            string response = null;
            const string responseType = "text/html";

            Interlocked.Increment(ref RequestCount);

            // Validate file name.
            if ("index.html" == file)
            {
                var path = $@"..\VotingServicePkg.Code.1.0.0\{file}";
                response = File.ReadAllText(path);
            }

            ServiceEventSource.Current.ServiceRequestStop("VotesController.GetFile", activityId);

            return null != response 
                ? Request.CreateResponse(HttpStatusCode.OK, response, responseType) 
                : Request.CreateErrorResponse(HttpStatusCode.NotFound, "File");
        }

        [HttpPost]
        [Route("api/{key}")]
        public HttpResponseMessage Post(string key)
        {
            var activityId = Guid.NewGuid().ToString();
            ServiceEventSource.Current.ServiceRequestStart("VotesController.Post", activityId);

            Interlocked.Increment(ref RequestCount);

            if (false == _counts.ContainsKey(key))
            {
                _counts.Add(key, 1);
            }
            else
            {
                _counts[key] = _counts[key] + 1;
            }

            ServiceEventSource.Current.ServiceRequestStop("VotesController.Post", activityId);

            return Request.CreateResponse(HttpStatusCode.NoContent);
        }

        #endregion
    }
}