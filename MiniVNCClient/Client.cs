using MiniVNCClient.Types;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MiniVNCClient
{
	public class Client
	{
		#region Fields
		private static readonly Regex _RegexServerVersion = new Regex(@"RFB (?<major>\d{3})\.(?<minor>\d{3})\n");
		private static readonly string _VersionFormat = "RFB {0:000}.{1:000}\n";
		private static readonly Version _ClientVersion = new Version(3, 8);
		private static readonly Dictionary<int, SecurityType> _SecurityTypes;
		private static readonly SecurityType[] _SupportedSecurityTypes = new[] { SecurityType.Invalid, SecurityType.None };

		private Stream _Stream;
		private BinaryReader _Reader;
		private BinaryWriter _Writer;
		#endregion

		#region Properties
		public Version ServerVersion { get; set; }

		public Version ClientVersion => _ClientVersion;
		#endregion

		#region Constructors
		static Client()
		{
			_SecurityTypes = Enumerable.Range(0, 256)
				.Select(
					i =>
					{
						SecurityType? type = null;

						switch (i)
						{
							case 0:
								type = SecurityType.Invalid;
								break;
							case 1:
								type = SecurityType.None;
								break;
							case 2:
								type = SecurityType.VNCAuthentication;
								break;
							case 5:
								type = SecurityType.RA2;
								break;
							case 6:
								type = SecurityType.RA2ne;
								break;
							case 16:
								type = SecurityType.Tight;
								break;
							case 17:
								type = SecurityType.Ultra;
								break;
							case 18:
								type = SecurityType.TLS;
								break;
							case 19:
								type = SecurityType.VeNCrypt;
								break;
							case 20:
								type = SecurityType.SASL;
								break;
							case 21:
								type = SecurityType.MD5;
								break;
							case 22:
								type = SecurityType.xvp;
								break;
							case 23:
								type = SecurityType.SecureTunnel;
								break;
							case 24:
								type = SecurityType.IntegratedSSH;
								break;
							default:
								if (
									(i >= 3 && i <= 4)
									||
									(i >= 7 && i <= 15)
									||
									(i >= 128 && i <= 255)
								)
								{
									type = SecurityType.RealVNC;
								}
								else if (i >= 30 && i <= 35)
								{
									type = SecurityType.Apple;
								}
								break;
						}

						return (value: i, type: type);
					}
				)
				.Where(v => v.type.HasValue)
				.ToDictionary(v => v.value, v => v.type.Value);
		}

		public Client()
		{
		}
		#endregion

		#region Private methods
		private void Initialize(Stream stream)
		{
			_Stream = stream;
			_Reader = new BinaryReader(_Stream);
			_Writer = new BinaryWriter(_Stream);

			NegotiateVersion();
			NegotiateAuthentication();
			InitializeSession();
		}

		private void NegotiateVersion()
		{
			var matchVersion = _RegexServerVersion.Match(Encoding.ASCII.GetString(_Reader.ReadBytes(12)));

			ServerVersion = new Version(int.Parse(matchVersion.Groups["major"].Value), int.Parse(matchVersion.Groups["minor"].Value));

			_Writer.Write(Encoding.ASCII.GetBytes(string.Format(_VersionFormat, _ClientVersion.Major, _ClientVersion.Minor)));
		}

		private void NegotiateAuthentication()
		{
			SecurityType[] securityTypes = new SecurityType[0];

			if (ServerVersion >= new Version(3, 7))
			{
				var totalSecurityTypes = _Reader.ReadByte();

				if (totalSecurityTypes > 0)
				{
					securityTypes = _Reader.ReadBytes(totalSecurityTypes)
						.Select(s => _SecurityTypes[s])
						.Where(s => _SupportedSecurityTypes.Contains(s))
						.Distinct()
						.ToArray();
				}
			}
			else
			{
				var securityTypeValue = _Reader.ReadUInt32();

				if (_SecurityTypes.ContainsKey((int)securityTypeValue))
				{
					var securityType = _SecurityTypes[(int)securityTypeValue];

					if (_SupportedSecurityTypes.Contains(securityType))
					{
						securityTypes = new[] { securityType };
					}
				}
			}

			if (securityTypes.Contains(SecurityType.Invalid))
			{
				var reasonLength = _Reader.ReadInt32();

				var reason = Encoding.ASCII.GetString(_Reader.ReadBytes(reasonLength));

				throw new Exception($"Connection failed: {reason}");
			}

			if (!securityTypes.Any())
			{
				throw new Exception("No supported security types");
			}

			if (securityTypes.Contains(SecurityType.None))
			{
				_Writer.Write((byte)SecurityType.None);

				if (ServerVersion >= new Version(3, 8))
				{
					var securityResult = (SecurityResult)_Reader.ReadUInt32();

					if (securityResult != SecurityResult.OK)
					{
						throw new Exception($"Connection failed: unknown reason");
					}
				}
			}
			else if (securityTypes.Contains(SecurityType.VNCAuthentication))
			{
				var challenge = _Reader.ReadBytes(16);

				throw new NotImplementedException("TODO: VNC Authentication");
			}
		}

		private void InitializeSession()
		{
			var clientInit = new ClientInit() { Shared = true };

			clientInit.Serialize(_Stream);

			var serverInit = ServerInit.Deserialize(_Stream);
		}
		#endregion

		#region Public methods
		public void Connect(string hostname, int port)
		{
			var tcpClient = new TcpClient();
			tcpClient.Connect(hostname, port);

			Initialize(tcpClient.GetStream());
		}
		#endregion
	}
}
