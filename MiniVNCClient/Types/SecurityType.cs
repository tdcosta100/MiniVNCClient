

namespace MiniVNCClient.Types
{
	public enum SecurityType
	{
		Invalid = 0,
		None = 1,
		VNCAuthentication = 2,
		RealVNC,
		RA2,
		RA2ne,
		Tight,
		Ultra,
		TLS,
		VeNCrypt,
		SASL,
		MD5,
		xvp,
		SecureTunnel,
		IntegratedSSH,
		Apple
	}
}
