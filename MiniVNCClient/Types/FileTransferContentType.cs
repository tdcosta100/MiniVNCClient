

namespace MiniVNCClient.Types
{
	public class FileTransferContentType
	{
		// FileTransfer Content types and Params defines
		public const int DirContentRequest = 1;
		// Client asks for the content of a given Server directory
		public const int DirPacket = 2; // Full directory name or full file name.
		// Null content means end of Directory
		public const int FileTransferRequest = 3;
		// Client asks the server for the tranfer of a given file
		public const int FileHeader = 4;
		// First packet of a file transfer, containing file's features
		public const int FilePacket = 5; // One slice of the file
		public const int EndOfFile = 6;
		// End of file transfer (the file has been received or error)
		public const int AbortFileTransfer = 7;
		// The file transfer must be aborted, whatever the state
		public const int FileTransferOffer = 8;
		// The client offers to send a file to the server
		public const int FileAcceptHeader = 9; // The server accepts or rejects the file
		public const int Command = 10;
		// The Client sends a simple command (File Delete, Dir create etc...)
		public const int CommandReturn = 11;
		//	New FT Protocole (V2) The zipped checksums of the destination file (Delta Transfer)
		public const int FileChecksums = 12;
		// The Client receives the server's answer about a simple command
		// rfbDirContentRequest client Request - content params 
		public const int RDirContent = 1; // Request a Server Directory contents
		public const int RDrivesList = 2; // Request the server's drives list

		// rfbDirPacket & rfbCommandReturn  server Answer - content params
		public const int ADirectory = 1; // Reception of a directory name
		public const int AFile = 2; // Reception of a file name 
		public const int ADrivesList = 3; // Reception of a list of drives
		public const int ADirCreate = 4; // Response to a create dir command 
		public const int ADirDelete = 5; // Response to a delete dir command 
		public const int AFileCreate = 6; // Response to a create file command 
		public const int AFileDelete = 7; // Response to a delete file command

		// rfbCommand Command - content params
		public const int CDirCreate = 1; // Request the server to create the given directory
		public const int CDirDelete = 2; // Request the server to delete the given directory
		public const int CFileCreate = 3; // Request the server to create the given file
		public const int CFileDelete = 4; // Request the server to delete the given file

		// Errors - content params or "size" field
		public const int RErrorUnknownCmd = 1; // Unknown FileTransfer command.
		public const int RErrorCmd = unchecked((int)0xFFFFFFFF);
	}
}
