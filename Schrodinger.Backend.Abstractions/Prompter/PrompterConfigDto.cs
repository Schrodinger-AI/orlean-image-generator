namespace Schrodinger.Backend.Abstractions.Prompter
{
    using System.Text.Json.Serialization;

    /// <summary>
    /// Represents the configuration for a prompter.
    /// </summary>
    [GenerateSerializer]
    public class PrompterConfigDto
    {
        /// <summary>
        /// Gets or sets the script content for the prompter.
        /// </summary>
        [Id(0)]
        public string ScriptContent { get; set; }

        /// <summary>
        /// Gets or sets the configuration text for the prompter.
        /// </summary>
        [Id(1)]
        public string ConfigText { get; set; }

        /// <summary>
        /// Gets or sets the validation test case for the prompter.
        /// </summary>
        [Id(2)]
        public string ValidationTestCase { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the validation is successful.
        /// </summary>
        [Id(3)]
        public bool ValidationOk { get; set; }
    }
}