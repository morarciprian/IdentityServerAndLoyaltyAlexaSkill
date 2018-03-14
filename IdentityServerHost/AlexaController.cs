using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Alexa.NET;
using Alexa.NET.Request;
using Alexa.NET.Request.Type;
using Alexa.NET.Response;
using IdentityServerHost.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using ServiceReference;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace IdentityServerHost
{
    [Route("api/alexa")]
    public class AlexaController : Controller
    {
        private readonly IDistributedCache _cache;
        List<PurchaseOrderItem> purchaseItems = new List<PurchaseOrderItem>();
        List<RedeemableItem> redeemableItems = new List<RedeemableItem>();

        public AlexaController(IDistributedCache cache)
        {
            _cache = cache;
        }

        [HttpPost]
        public async Task<dynamic> Post([FromBody]SkillRequest input)
        {

            var speech = new Alexa.NET.Response.SsmlOutputSpeech();
            var finalResponse = new SkillResponse();
            finalResponse.Version = "1.0";
            // check what type of a request it is like an IntentRequest or a LaunchRequest
            var requestType = input.GetRequestType();

            if (requestType == typeof(IntentRequest))
            {
                // do some intent-based stuff
                var intentRequest = input.Request as IntentRequest;

                // check the name to determine what you should do
                if (intentRequest.Intent.Name.Equals("GetPoints"))
                {
                    try
                    {
                        long? points = 0;
                        var client = new CustomApiClient();
                        var result = await client.QueryPointsAsync("API", "EXTERNAL", 1, "921722255", ParamType.Msisdn, 1, null, "WALLET_DEFAULT", null, null, false, null, null, null);
                        if (result != null)
                        {
                            foreach (var wallet in result.Wallets)
                            {
                                points = points + wallet.Points;
                            }
                        }
                        //// create the speech response - cards still need a voice response
                        //speech.Ssml = $"<speak>You currently have {points} loyalty points available in your account.</speak>";
                        //// create the card response
                        //finalResponse = ResponseBuilder.TellWithCard(speech, "GetPoints", $"You currently have {points} loyalty points available in your account.");

                        //_cache.sa
                        // create the speech response - cards still need a voice response
                        speech.Ssml = $"<speak>You currently have {points} loyalty points available in your account.</speak>";

                        // create the speech reprompt
                        var repromptMessage = new PlainTextOutputSpeech();
                        repromptMessage.Text = "Anything else you might want to do?";

                        // create the reprompt
                        var repromptBody = new Alexa.NET.Response.Reprompt();
                        repromptBody.OutputSpeech = repromptMessage;

                        // create the response
                        finalResponse = ResponseBuilder.AskWithCard(speech, "GetPoints", $"You currently have {points} loyalty points available in your account.", repromptBody);
                    }
                    catch (Exception e)
                    {
                        // create the speech response - cards still need a voice response
                        speech.Ssml = $"<speak>Oh boy, something went very wrong. {e.Message}</speak>";
                        // create the card response
                        finalResponse = ResponseBuilder.TellWithCard(speech, "GetPoints Exception", $"Oh boy, something went very wrong. {e.Message}.");
                    }

                }
                // check the name to determine what you should do
                if (intentRequest.Intent.Name.Equals("GetItems"))
                {
                    try
                    {
                        string items = string.Empty;
                        SessionData sessionData = null;
                        var client = new CustomApiClient();
                        var result = await client.QueryAvailableItemsAsync("API", "EXTERNAL", 1, "921722255", ParamType.Msisdn, 1, null, null, null, null, null);
                        if (result != null)
                        {
                            //items = string.Join(", ", result.Items.Select(z => z.Name));
                            redeemableItems.AddRange(result.Items);
                            items = string.Join(", ", redeemableItems.Select(z => z.Name));
                            var value = await _cache.GetStringAsync(input.Session.SessionId);
                            if (value != null)
                            {
                                sessionData = JsonConvert.DeserializeObject<SessionData>(value);
                            }
                            if (sessionData != null)
                            {
                                sessionData.RedeemableItems = redeemableItems;
                            }
                            else
                            {
                                sessionData = new SessionData { SessionId = input.Session.SessionId, PurchaseItems = purchaseItems, RedeemableItems = redeemableItems };
                            }
                            await _cache.SetStringAsync(input.Session.SessionId, JsonConvert.SerializeObject(sessionData));
                        }
                        // create the speech response - cards still need a voice response
                        speech.Ssml = $"<speak>Here is the list of items available for you: {items}.</speak>";
                        // create the card response
                        //finalResponse = ResponseBuilder.TellWithCard(speech, "GetItems", $"Here is the list of items available for you: {items}.");

                        // create the speech reprompt
                        var repromptMessage = new PlainTextOutputSpeech();
                        repromptMessage.Text = "Would you like to add any of these to your shoping cart?";

                        // create the reprompt
                        var repromptBody = new Alexa.NET.Response.Reprompt();
                        repromptBody.OutputSpeech = repromptMessage;

                        // create the response
                        finalResponse = ResponseBuilder.AskWithCard(speech, "GetItems", $"Here is the list of items available for you: {items}.", repromptBody);
                    }
                    catch (Exception e)
                    {
                        // create the speech response - cards still need a voice response
                        speech.Ssml = $"<speak>Oh boy, something went very wrong. {e.Message}</speak>";
                        // create the card response
                        finalResponse = ResponseBuilder.TellWithCard(speech, "GetItems Exception", $"Oh boy, something went very wrong. {e.Message}.");
                    }

                }
                // check the name to determine what you should do
                if (intentRequest.Intent.Name.Equals("AddToBasket"))
                {
                    try
                    {
                        SessionData sessionData = null;
                        RedeemableItem itemR = null;
                        PurchaseOrderItem itemP = null;
                        string itemName = string.Empty;
                        int itemNo = int.Parse(intentRequest.Intent.Slots["Item"].Value);


                        var value = await _cache.GetStringAsync(input.Session.SessionId);
                        if (value != null)
                        {
                            sessionData = JsonConvert.DeserializeObject<SessionData>(value);
                        }

                        if (sessionData != null)
                        {
                            if (sessionData.RedeemableItems != null && sessionData.RedeemableItems.Count > 0)
                            {
                                redeemableItems = sessionData.RedeemableItems;
                                if ((itemNo - 1) > sessionData.RedeemableItems.Count || (itemNo - 1) < 0)
                                {
                                    throw new Exception("Sorry, you don't have that item.");
                                }
                                itemR = redeemableItems[itemNo - 1];
                                if (sessionData.PurchaseItems != null)
                                {
                                    purchaseItems = sessionData.PurchaseItems;
                                    purchaseItems.Add(new PurchaseOrderItem { DeliveryChannel = null, Quantity = 1, RedeemableItemId = itemR.Id, WalletType = new WalletType { ExternalCode = "WALLET_DEFAULT" } });
                                }
                                //else
                                //{
                                //    purchaseItems.Add(new PurchaseOrderItem { DeliveryChannel = null, Quantity = 1, RedeemableItemId = itemR.Id, WalletType = null });
                                //}
                            }
                            else
                            {
                                throw new Exception("Please query for your ityems first.");
                            }

                        }
                        else
                        {
                            sessionData = new SessionData { SessionId = input.Session.SessionId, PurchaseItems = purchaseItems, RedeemableItems = redeemableItems };
                        }
                        await _cache.SetStringAsync(input.Session.SessionId, JsonConvert.SerializeObject(sessionData));


                        purchaseItems.Add(new PurchaseOrderItem { DeliveryChannel = null, Quantity = 1, RedeemableItemId = itemR.Id, WalletType = null });

                        // create the speech response - cards still need a voice response
                        speech.Ssml = $"<speak>Your item, {itemR.Name}, was successfully added to shopping cart.</speak>";

                        // create the speech reprompt
                        var repromptMessage = new PlainTextOutputSpeech();
                        repromptMessage.Text = "Shall I proceed and buy the items in the shoping cart? You can add more items as well.";

                        // create the reprompt
                        var repromptBody = new Alexa.NET.Response.Reprompt();
                        repromptBody.OutputSpeech = repromptMessage;

                        // create the response
                        finalResponse = ResponseBuilder.AskWithCard(speech, "AddToBasket", $"Your item, {itemR.Name}, was successfully added to shopping cart.", repromptBody);
                    }
                    catch (Exception e)
                    {
                        // create the speech response - cards still need a voice response
                        speech.Ssml = $"<speak>Oh boy, something went very wrong. {e.Message}</speak>";
                        // create the card response
                        finalResponse = ResponseBuilder.TellWithCard(speech, "AddToBasket Exception", $"Oh boy, something went very wrong. {e.Message}.");
                    }
                }
                // check the name to determine what you should do
                if (intentRequest.Intent.Name.Equals("PurchaseBasket"))
                {
                    try
                    {
                        SessionData sessionData = null;
                        string status = string.Empty;

                        var value = await _cache.GetStringAsync(input.Session.SessionId);
                        if (value != null)
                        {
                            sessionData = JsonConvert.DeserializeObject<SessionData>(value);
                        }
                        if (sessionData != null && sessionData.PurchaseItems != null && sessionData.PurchaseItems.Count > 0)
                        {
                            purchaseItems = sessionData.PurchaseItems;
                        }
                        else
                        {
                            throw new Exception("Your shoping cart is empty. Add some items first.");
                        }

                        await _cache.RemoveAsync(input.Session.SessionId);

                        var client = new CustomApiClient();
                        // var result = await client.RedeemItemsAsync("API", "EXTERNAL", 1, "921722255", ParamType.Msisdn, 1, null, purchaseItems.ToArray(), null, null, null);
                        var result = await client.RedeemItemsAsync("API", "EXTERNAL", 1, "921722255", ParamType.Msisdn, 1, null, purchaseItems.ToArray(), null, null, null);
                        if (result != null)
                        {
                            status = result.Status.ToString();
                        }

                        // create the speech response - cards still need a voice response
                        speech.Ssml = $"<speak>Your order completed with {status}. Thank you for using our services.</speak>";

                        // create the response
                        finalResponse = ResponseBuilder.TellWithCard(speech, "PurchaseBasket", $"Your order completed with {status}. Thank you for using our services.");
                    }
                    catch (Exception e)
                    {
                        // create the speech response - cards still need a voice response
                        speech.Ssml = $"<speak>Oh boy, something went very wrong. {e.Message}</speak>";
                        // create the card response
                        finalResponse = ResponseBuilder.TellWithCard(speech, "PurchaseBasket Exception", $"Oh boy, something went very wrong. {e.Message}.");
                    }
                }
                if (intentRequest.Intent.Name.Equals("AMAZON.CancelIntent"))
                {
                    try
                    {
                        await _cache.RemoveAsync(input.Session.SessionId);

                        List<string> myList = new List<string> { "OK,I'll shut up.", "Sure, I'll cleanup everything.", "Oh boy, that escalated quickly! I'm outa here!" };
                        // add items to the list
                        Random r = new Random();
                        int index = r.Next(myList.Count);

                        // create the speech response - cards still need a voice response
                        speech.Ssml = $"<speak>{myList[index]}</speak>";

                        // create the response
                        finalResponse = ResponseBuilder.TellWithCard(speech, "Cancel Exception", $"{myList[index]}");

                    }
                    catch (Exception e)
                    {
                        // create the speech response - cards still need a voice response
                        speech.Ssml = $"<speak>Oh boy, something went very wrong. {e.Message}</speak>";
                        // create the card response
                        finalResponse = ResponseBuilder.TellWithCard(speech, "Cancel Exception", $"Oh boy, something went very wrong. {e.Message}.");
                    }

                }

                if (intentRequest.Intent.Name.Equals("Hello"))
                {
                    try
                    {
                        await _cache.RemoveAsync(input.Session.SessionId);

                        List<string> myList = new List<string> { "Hi there!", "Hi, how are you.", "Hi and goodby. get back to work" };
                        // add items to the list
                        Random r = new Random();
                        int index = r.Next(myList.Count);

                        // create the speech response - cards still need a voice response
                        speech.Ssml = $"<speak>{myList[index]}</speak>";

                        // create the response
                        finalResponse = ResponseBuilder.TellWithCard(speech, "Hello", $"{myList[index]}");

                    }
                    catch (Exception e)
                    {
                        // create the speech response - cards still need a voice response
                        speech.Ssml = $"<speak>Oh boy, something went very wrong. {e.Message}</speak>";
                        // create the card response
                        finalResponse = ResponseBuilder.TellWithCard(speech, "Cancel Exception", $"Oh boy, something went very wrong. {e.Message}.");
                    }

                }
            }
            else if (requestType == typeof(Alexa.NET.Request.Type.LaunchRequest))
            {
                // default launch path executed
            }
            else if (requestType == typeof(AudioPlayerRequest))
            {
                // do some audio response stuff
            }

            return finalResponse;

        }
    }
}

