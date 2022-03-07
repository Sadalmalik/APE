namespace Processor
{
	public abstract class Device
	{
		public int Interrupt { get; protected set; }

		public virtual byte GetValueByte(ulong address){ return 0; }
		public virtual void  SetValueByte(ulong address, byte value) {}
		
		public virtual ushort GetValueWord(ulong address) { return 0; }
		public virtual void SetValueWord(ulong address, ushort value) {}
		
		public virtual uint GetValueDWord(ulong address) { return 0; }
		public virtual void  SetValueDWord(ulong address, uint value) {}
		
		public virtual ulong GetValueQWord(ulong address) { return 0; }

		public virtual void SetValueQWord(ulong address, ulong value){}

		public virtual void GetValue(ulong address, Register reg, int size)
		{
			switch (size)
			{
				case Size.Byte: reg.dBYTE = GetValueByte(address); break;
				case Size.Word: reg.dWORD = GetValueWord(address); break;
				case Size.DWord: reg.dDWORD = GetValueDWord(address); break;
				case Size.QWord: reg.QWORD = GetValueQWord(address); break;
			}
		}

		public virtual void SetValue(ulong address, Register reg, int size)
		{
			switch (size)
			{
				case Size.Byte: SetValueByte(address, reg.dBYTE); break;
				case Size.Word: SetValueWord(address, reg.dWORD); break;
				case Size.DWord: SetValueDWord(address, reg.dDWORD); break;
				case Size.QWord: SetValueQWord(address, reg.QWORD); break;
			}
		}
	}
}