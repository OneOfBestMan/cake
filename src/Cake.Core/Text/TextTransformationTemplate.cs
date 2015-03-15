﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Cake.Core.Text
{
    /// <summary>
    /// Provides template functionality for simple text transformations.
    /// </summary>
    public sealed class TextTransformationTemplate : ITextTransformationTemplate
    {
        private readonly Dictionary<string, object> _tokens;
        private readonly string _template;
        private readonly Tuple<string, string> _placeholder;
        private readonly string _keyExpression;

        /// <summary>
        /// Initializes a new instance of the <see cref="TextTransformationTemplate"/> class.
        /// </summary>
        /// <param name="template">The template.</param>
        public TextTransformationTemplate(string template)
            : this(template, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TextTransformationTemplate"/> class.
        /// </summary>
        /// <param name="template">The template.</param>
        /// <param name="placeholder">The key placeholder.</param>
        public TextTransformationTemplate(string template, Tuple<string, string> placeholder)
        {
            if (template == null)
            {
                throw new ArgumentNullException("template");
            }
            _template = template;
            _placeholder = placeholder ?? new Tuple<string, string>("<%", "%>");
            _tokens = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            _keyExpression = string.Concat(_placeholder.Item1, @"(?<key>[^", _placeholder.Item2[0], "]+)", _placeholder.Item2);
        }

        /// <summary>
        /// Registers a key and an associated value.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void Register(string key, object value)
        {
            if (key == null)
            {
                throw new ArgumentNullException("key");
            }
            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Key cannot be empty.", "key");
            }
            if (_tokens.ContainsKey(key))
            {
                const string format = "The key '{0}' has already been added.";
                var message = string.Format(CultureInfo.InvariantCulture, format, key);
                throw new InvalidOperationException(message);
            }
            _tokens.Add(key, value);
        }

        /// <summary>
        /// Renders the template using the registered tokens.
        /// </summary>
        /// <returns>The rendered template.</returns>
        public string Render()
        {
            return Regex.Replace(_template, _keyExpression, Replace);
        }

        private string Replace(Match match)
        {
            var expression = match.Groups["key"].Value;
            var parts = expression.Split(new[] { ':' }, StringSplitOptions.None);
            var key = parts[0].Trim();
            if (_tokens.ContainsKey(key))
            {
                // Get the value.
                var value = _tokens[key];
                if (value == null)
                {
                    return string.Empty;
                }
                if (parts.Length > 1)
                {
                    // Formattable?
                    var format = string.Join(":", parts.Skip(1).Take(parts.Length - 1)).Trim();
                    var formattable = _tokens[key] as IFormattable;
                    if (formattable != null)
                    {
                        return formattable.ToString(format, CultureInfo.InvariantCulture);
                    }

                    // Return what we received.
                    return match.Value;
                }
                return value.ToString();
            }

            // Return what we received.
            return match.Value;
        }
    }
}