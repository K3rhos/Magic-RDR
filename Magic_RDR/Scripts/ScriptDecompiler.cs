using System;
using System.IO;
using Magic_RDR.Application;
using Magic_RDR.RPF;
using static Magic_RDR.RPF6.RPF6TOC;



namespace Magic_RDR
{
	public class ScriptDecompiler
	{
		private readonly IOReader Reader;
		private readonly FileEntry FileEntry;
		private readonly string DecompiledCode;

		public ScriptDecompiler(TOCSuperEntry entry)
		{
			FileEntry = entry.Entry.AsFile;

			RPFFile.RPFIO.Position = FileEntry.GetOffset();

			byte[] fileData = ResourceUtils.ResourceInfo.GetDataFromResourceBytes(RPFFile.RPFIO.ReadBytes(FileEntry.SizeInArchive));

			Reader = new IOReader(new MemoryStream(fileData), (AppGlobals.Platform == AppGlobals.PlatformEnum.Switch) ? IOReader.Endian.Little : IOReader.Endian.Big);
			Reader.BaseStream.Seek(FileEntry.FlagInfo.RSC85_ObjectStart, SeekOrigin.Begin);

			ScriptFile script = new ScriptFile(Reader, FileEntry);

			DecompiledCode = script.ReadMainStructure();
		}

		public void Export(string filePath, bool nativeNamespace = false)
		{
			bool prevValue = NativeHashDB.ShowNativeNamespace;

			NativeHashDB.ShowNativeNamespace = nativeNamespace;

			File.WriteAllText(filePath, DecompiledCode);

			NativeHashDB.ShowNativeNamespace = prevValue;
		}
	}
}
