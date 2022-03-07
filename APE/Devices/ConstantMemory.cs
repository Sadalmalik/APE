using System;

namespace Processor
{
	public class ConstantMemory : Memory
	{
		public ConstantMemory(byte[] content) : base(-1)
		{
			Storage = content;
			Capacity = content.Length;
		}
		
#region Ignore writing

		public override void SetValueByte(ulong address, byte value)
		{
			Interrupt = 1;
		}


		public override void SetValueWord(ulong address, ushort value)
		{
			Interrupt = 1;
		}

		public override void SetValueDWord(ulong address, uint value)
		{
			Interrupt = 1;
		}


		public override void SetValueQWord(ulong address, ulong value)
		{
			Interrupt = 1;
		}

#endregion
	}
}