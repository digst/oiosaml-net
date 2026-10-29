using System;

namespace DK.Gov.Oio.Saml.Exceptions;

// The message is not well-formed SAML, e.g. not base64 or not XML.
public class Saml20FormatException(string msg, Exception? cause = null) : Exception(msg, cause);
