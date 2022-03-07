namespace Processor
{
	public static class Size
	{
		public const int Byte = 0;
		public const int Word  = 1;
		public const int DWord  = 2;
		public const int QWord  = 3;

		public static readonly ulong[] SizeToStep = { 1, 2, 4, 8};
	}
}