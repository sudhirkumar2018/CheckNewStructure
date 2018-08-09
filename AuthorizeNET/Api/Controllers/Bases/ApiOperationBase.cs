namespace AuthorizeNet.Api.Controllers.Bases
{
    using System.Collections.Generic;
    using System.Globalization;
    using System;
    using Contracts.V1;
    using Utilities;
    using Microsoft.Extensions.Logging;

    public abstract class ApiOperationBase<TQ, TS> : IApiOperation<TQ, TS>
            where TQ : ANetApiRequest
            where TS : ANetApiResponse
    {
        protected static ILogger Logger = LogFactory.getLog(typeof(ApiOperationBase<TQ, TS>));

        public static AuthorizeNet.Environment RunEnvironment { get; set; }
        public static merchantAuthenticationType MerchantAuthentication { get; set; }

        private TQ _apiRequest;
        private TS _apiResponse;

        readonly Type _requestClass;
        readonly Type _responseClass;

        private ANetApiResponse _errorResponse;

        protected ApiOperationBase(TQ apiRequest)
        {
            if (null == apiRequest)
            {
                Logger.LogError("null apiRequest");
                throw new ArgumentNullException("apiRequest", "Input request cannot be null");
            }
            if (null != GetApiResponse())
            {
                Logger.LogError(GetApiResponse().ToString());
                throw new InvalidOperationException("Response should be null");
            }

            _requestClass = typeof(TQ);//Type<TQ>) ((ParameterizedType) getClass().getGenericSuperclass()).getActualTypeArguments()[0];
            _responseClass = typeof(TS);// GetResponseType();
            SetApiRequest(apiRequest);

            Logger.LogDebug("Creating instance for request:'{0}' and response:'{1}'", _requestClass, _responseClass);
            //Logger.debug(string.Format("Request:'{0}'", apiRequest));
            //Logger.debug(string.Format("Request(Ctor):'{0}'", XmlUtility.GetXml(apiRequest)));
            Validate();
        }

        protected TQ GetApiRequest()
        {
            return _apiRequest;
        }

        protected void SetApiRequest(TQ apiRequest)
        {
            _apiRequest = apiRequest;
        }

        public TS GetApiResponse()
        {
            return _apiResponse;
        }

        private void SetApiResponse(TS apiResponse)
        {
            _apiResponse = apiResponse;
        }

        public ANetApiResponse GetErrorResponse()
        {
            return _errorResponse;
        }

        private void SetErrorResponse(ANetApiResponse errorResponse)
        {
            _errorResponse = errorResponse;
        }

        public TS ExecuteWithApiResponse(AuthorizeNet.Environment environment = null)
        {
            Execute(environment);
            return GetApiResponse();
        }

        const String NullEnvironmentErrorMessage = "Environment not set. Set environment using setter or use overloaded method to pass appropriate environment";

        public void Execute(AuthorizeNet.Environment environment = null)
        {
            BeforeExecute();

            //Logger.debug(string.Format(CultureInfo.InvariantCulture, "Executing Request:'{0}'", XmlUtility.GetXml(GetApiRequest())));

            if (null == environment) { environment = ApiOperationBase<ANetApiRequest, ANetApiResponse>.RunEnvironment; }
            if (null == environment) throw new ArgumentException(NullEnvironmentErrorMessage);

            var httpApiResponse = HttpUtility.PostData<TQ, TS>(environment, GetApiRequest());

            if (null != httpApiResponse)
            {
                Logger.LogDebug("Received Response:'{0}' for request:'{1}'", httpApiResponse, GetApiRequest());
                if (httpApiResponse.GetType() == _responseClass)
                {
                    var response = (TS)httpApiResponse;
                    SetApiResponse(response);
                    Logger.LogDebug("Setting response: '{0}'", response);
                }
                else if (httpApiResponse.GetType() == typeof(ErrorResponse))
                {
                    SetErrorResponse(httpApiResponse);
                    Logger.LogDebug("Received ErrorResponse:'{0}'", httpApiResponse);
                }
                else
                {
                    SetErrorResponse(httpApiResponse);
                    Logger.LogError("Invalid response:'{0}'", httpApiResponse);
                }
                Logger.LogDebug("Response obtained: {0}", GetApiResponse());
                SetResultStatus();

            }
            else
            {
                Logger.LogDebug("Got a 'null' Response for request:'{0}'\n", GetApiRequest());
            }
            AfterExecute();
        }

        public messageTypeEnum GetResultCode()
        {
            return ResultCode;
        }

        private void SetResultStatus()
        {
            Results = new List<string>();
            var messageTypes = GetResultMessage();

            if (null != messageTypes)
            {
                ResultCode = messageTypes.resultCode;
            }

            if (null != messageTypes)
            {
                foreach (var amessage in messageTypes.message)
                {
                    Results.Add(string.Format(CultureInfo.InvariantCulture, "{0}:{1}", amessage.code, amessage.text));
                }
            }
        }

        public List<string> GetResults()
        {
            return Results;
        }

        private messagesType GetResultMessage()
        {
            messagesType messageTypes = null;

            if (null != GetErrorResponse())
            {
                messageTypes = GetErrorResponse().messages;
            }
            else if (null != GetApiResponse())
            {
                messageTypes = GetApiResponse().messages;
            }

            return messageTypes;
        }

        protected List<string> Results = null;
        protected messageTypeEnum ResultCode = messageTypeEnum.Ok;

        protected virtual void BeforeExecute() { }
        protected virtual void AfterExecute() { }

        protected abstract void ValidateRequest();

        private void Validate()
        {

            ANetApiRequest request = GetApiRequest();

            //validate not nulls
            ValidateAndSetMerchantAuthentication();

            //set the client Id
            SetClientId();

            //validate nulls
            var merchantAuthenticationType = request.merchantAuthentication;
            //if ( null != ) throw new IllegalArgumentException(" needs to be null");

            //TODO
            /*
		    if ( null != merchantAuthenticationType.Item.GetType().   sessionToken) throw new IllegalArgumentException("SessionToken needs to be null");
		    if ( null != merchantAuthenticationType.getPass_word()) throw new IllegalArgumentException("Pass_word needs to be null");
		    if ( null != merchantAuthenticationType.getMobileDeviceId()) throw new IllegalArgumentException("MobileDeviceId needs to be null");
             
	    
	        var impersonationAuthenticationType = merchantAuthenticationType.impersonationAuthentication;
		    if ( null != impersonationAuthenticationType) throw new IllegalArgumentException("ImpersonationAuthenticationType needs to be null");
            */
            //	    impersonationAuthenticationType.setPartnerLoginId(CnpApiLoginIdKey);
            //	    impersonationAuthenticationType.setPartnerTransactionKey(CnpTransactionKey);
            //	    merchantAuthenticationType.setImpersonationAuthentication(impersonationAuthenticationType);

            ValidateRequest();
        }

        private void ValidateAndSetMerchantAuthentication()
        {
            ANetApiRequest request = GetApiRequest();

            if (null == request.merchantAuthentication)
            {
                if (null != ApiOperationBase<ANetApiRequest, ANetApiResponse>.MerchantAuthentication)
                {
                    request.merchantAuthentication = ApiOperationBase<ANetApiRequest, ANetApiResponse>.MerchantAuthentication;
                }
                else
                {
                    throw new ArgumentException("MerchantAuthentication cannot be null");
                }
            }
        }

        private void SetClientId()
        {
            ANetApiRequest request = GetApiRequest();
            request.clientId = "sdk-dotnet-" + Constants.SDKVersion;
        }
    }

}
