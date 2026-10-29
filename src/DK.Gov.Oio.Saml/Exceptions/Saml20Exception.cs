using System;

namespace DK.Gov.Oio.Saml.Exceptions;

// Thrown deliberately by the library, to tell its errors apart from bugs.
public class Saml20Exception(string msg, Exception? cause = null) : Exception(msg, cause);

public class Saml20ValidationException(string msg, Exception? cause = null) : Exception(msg, cause);
