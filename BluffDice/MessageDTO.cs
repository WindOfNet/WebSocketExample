using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace BluffDice
{
    public enum ClientMessageType { Roll, Bluff, Debluff }
    public enum ToClientMessageType { Id, WaitRoll, RollResult, ApponentBluff, WaitBluff, BluffNotValid, BluffSuccess, PlayerLeave, WinResult }

    public class ClientMessage
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ClientMessageType Type { get; set; }
        public dynamic Message { get; set; }
    }

    public class ToClientMessage
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public ToClientMessageType Type { get; set; }
        public dynamic Message { get; set; }
        public bool CanRoll { get; set; }
        public bool CanBluff { get; set; }
        public bool CanDebluff { get; set; }
    }
}
