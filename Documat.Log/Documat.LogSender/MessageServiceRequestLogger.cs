using System;
using System.Collections.Generic;
using ServiceStack;
using ServiceStack.Host;
using ServiceStack.Messaging;
using ServiceStack.Web;
using System.Linq;
using System.Threading;

namespace Documat.LogSender
{
    /// <summary>
    /// WG 2016
    /// Custom Request logger that can publish logs using rabbitmq 
    /// </summary>
    /// <seealso cref="ServiceStack.Web.IRequestLogger" />
    public class MessageServiceRequestLogger : IRequestLogger
    {
        private static int requestId = 0;

        private readonly string _component;

        public MessageServiceRequestLogger(string component)
        {
            this._component = component;
        }

        /// <summary>
        /// Gets or sets the message service.
        /// </summary>
        /// <value>
        /// The message service.
        /// </value>
        public IMessageService MessageService { get; set; }

        /// <summary>
        /// Turn On/Off Tracking of Exceptions
        /// </summary>
        public bool EnableErrorTracking { get; set; }

        /// <summary>
        /// Turn On/Off Raw Request Body Tracking
        /// </summary>
        public bool EnableRequestBodyTracking { get; set; }

        /// <summary>
        /// Turn On/Off Tracking of Responses
        /// </summary>
        public bool EnableResponseTracking { get; set; }

        /// <summary>
        /// Turn On/Off Session Tracking
        /// </summary>
        public bool EnableSessionTracking { get; set; }

        /// <summary>
        /// Don't log requests of these types.
        /// </summary>
        public Type[] ExcludeRequestDtoTypes { get; set; }

        /// <summary>
        /// Don't log request bodys for services with sensitive information.
        /// By default Auth and Registration requests are hidden.
        /// </summary>
        public Type[] HideRequestBodyForRequestDtoTypes { get; set; }

        /// <summary>
        /// Limit access to /requestlogs service to role
        /// </summary>
        public string[] RequiredRoles { get; set; }

        /// <summary>
        /// Logs the specified request.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="requestDto">The request dto.</param>
        /// <param name="response">The response.</param>
        /// <param name="requestDuration">Duration of the request.</param>
        public void Log(IRequest request,
                                 object requestDto,
                                 object response,
                                 TimeSpan requestDuration)
        {
            var requestType = requestDto != null ? requestDto.GetType() : null;

            if (ExcludeRequestType(requestType))
                return;

            var requestLogEntry = CreateEntry(request, requestDto, response, requestDuration, requestType);

            requestLogEntry.Items.Add("Component",
                                      this._component);

            using (var messageProducer = this.MessageService.CreateMessageProducer())
            {
                try
                {
                    messageProducer.Publish(requestLogEntry);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        }

        /// <summary>
        /// Excludes the type of the request.
        /// </summary>
        /// <param name="requestType">Type of the request.</param>
        /// <returns></returns>
        protected bool ExcludeRequestType(Type requestType)
        {
            return ExcludeRequestDtoTypes != null
                   && requestType != null
                   && ExcludeRequestDtoTypes.Contains(requestType);
        }

        /// <summary>
        /// Creates the entry.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="requestDto">The request dto.</param>
        /// <param name="response">The response.</param>
        /// <param name="requestDuration">Duration of the request.</param>
        /// <param name="requestType">Type of the request.</param>
        /// <returns></returns>
        protected RequestLogEntry CreateEntry(IRequest request, object requestDto, object response, TimeSpan requestDuration, Type requestType)
        {
            var entry = new RequestLogEntry
            {
                Id = Interlocked.Increment(ref requestId),
                DateTime = DateTime.UtcNow,
                RequestDuration = requestDuration,
            };

            if (request != null)
            {
                entry.HttpMethod = request.Verb;
                entry.AbsoluteUri = request.AbsoluteUri;
                entry.PathInfo = request.PathInfo;
                entry.IpAddress = request.UserHostAddress;
                entry.ForwardedFor = request.Headers[HttpHeaders.XForwardedFor];
                entry.Referer = request.Headers[HttpHeaders.Referer];
                entry.Headers = request.Headers.ToDictionary();
                entry.UserAuthId = request.GetItemOrCookie(HttpHeaders.XUserAuthId);
                entry.SessionId = request.GetSessionId();
                entry.Items = SerializableItems(request.Items);
                entry.Session = EnableSessionTracking ? request.GetSession() : null;
            }

            if (HideRequestBodyForRequestDtoTypes != null
                && requestType != null
                && !HideRequestBodyForRequestDtoTypes.Contains(requestType))
            {
                entry.RequestDto = requestDto;
                if (request != null)
                {
                    entry.FormData = request.FormData.ToDictionary();

                    if (EnableRequestBodyTracking)
                    {
                        entry.RequestBody = request.GetRawBody();
                    }
                }
            }
            if (!response.IsErrorResponse())
            {
                if (EnableResponseTracking)
                    entry.ResponseDto = response;
            }
            else
            {
                if (EnableErrorTracking)
                    entry.ErrorResponse = ToSerializableErrorResponse(response);
            }

            return entry;
        }

        /// <summary>
        /// Serializables the items.
        /// </summary>
        /// <param name="items">The items.</param>
        /// <returns></returns>
        public Dictionary<string, string> SerializableItems(Dictionary<string, object> items)
        {
            var to = new Dictionary<string, string>();
            foreach (var item in items)
            {
                var value = item.Value == null
                    ? "(null)"
                    : item.Value.ToString();

                to[item.Key] = value;
            }

            return to;
        }

        /// <summary>
        /// Gets the latest logs.
        /// </summary>
        /// <param name="take">The take.</param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public List<RequestLogEntry> GetLatestLogs(int? take)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// To the serializable error response.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <returns></returns>
        private static object ToSerializableErrorResponse(object response)
        {
            var errorResult = response as IHttpResult;
            if (errorResult != null)
                return errorResult.Response;

            var ex = response as Exception;
            return ex != null ? ex.ToResponseStatus() : null;
        }
    }
}
