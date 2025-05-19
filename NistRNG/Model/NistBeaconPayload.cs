using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NistRNG.Model
{
    /// <summary>
    /// NIST Randomness request response payload
    /// </summary>
    public class NistBeaconPayload
    {
        /// <summary>
        /// Pulse data
        /// </summary>
        [JsonProperty("pulse")]
        public Pulse Pulse { get; set; }

    }

    /// <summary>
    /// NIST Randomness Pulse
    /// </summary>
    public class Pulse
    {
        /// <summary>
        /// Uri
        /// </summary>
        [JsonProperty("uri")]
        public string Uri { get; set; }

        /// <summary>
        /// Version
        /// </summary>
        [JsonProperty("version")]
        public double Version { get; set; }

        /// <summary>
        /// Cipher Suite
        /// </summary>
        [JsonProperty("cipherSuite")]
        public double CipherSuite { get; set; }

        /// <summary>
        /// Refresh period in milliseconds
        /// </summary>
        [JsonProperty("period")]
        public double Period { get; set; }

        /// <summary>
        /// Certificate Id
        /// </summary>
        [JsonProperty("certificateId")]
        public string CertificateId { get; set; }

        /// <summary>
        /// Chain Index
        /// </summary>
        [JsonProperty("chainIndex")]
        public double ChainIndex { get; set; }

        /// <summary>
        /// Pulse Index
        /// </summary>
        [JsonProperty("pulseIndex")]
        public double PulseIndex { get; set; }


        /// <summary>
        /// TimeStamp
        /// </summary>
        [JsonProperty("timeStamp")]
        public DateTime TimeStamp { get; set; }

        /// <summary>
        /// Local Random Value
        /// </summary>
        [JsonProperty("localRandomValue")]
        public string LocalRandomValue { get; set; }

        /// <summary>
        /// External
        /// </summary>
        [JsonProperty("external")]
        public RandomnessSource External { get; set; }

        /// <summary>
        /// List Values
        /// </summary>
        [JsonProperty("listValues")]
        public List<SampleValue> ListValues { get; set; }

        /// <summary>
        /// Precommitment Value
        /// </summary>
        [JsonProperty("precommitmentValue")]
        public string PrecommitmentValue { get; set; }

        /// <summary>
        /// Status Code
        /// </summary>
        [JsonProperty("statusCode")]
        public double StatusCode { get; set; }

        /// <summary>
        /// Signature Value
        /// </summary>
        [JsonProperty("signatureValue")]
        public string SignatureValue { get; set; }

        /// <summary>
        /// Output Value
        /// </summary>
        [JsonProperty("outputValue")]
        public string OutputValue { get; set; }

    }

    public class RandomnessSource
    {
        /// <summary>
        /// SourceId
        /// </summary>
        [JsonProperty("sourceId")]
        public int SourceId { get; set; }

        /// <summary>
        /// Status Code
        /// </summary>
        [JsonProperty("statusCode")]
        public double StatusCode { get; set; }

        /// <summary>
        /// Value
        /// </summary>
        [JsonProperty("value")]
        public int Value { get; set; }

    }

    public class SampleValue
    {
        /// <summary>
        /// Uri
        /// </summary>
        [JsonProperty("uri")]
        public string Uri { get; set; }

        /// <summary>
        /// Type
        /// </summary>
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// Value
        /// </summary>
        [JsonProperty("value")]
        public string Value { get; set; }
    }

}
