namespace Processor
{
	public class Memory : Device
	{
		public int    Capacity { get; protected set; }
		public byte[] Storage  { get; protected set; }

		public Memory(int capacity = 1024 * 1024 * 16)
		{
			Capacity = capacity;
			if (capacity >= 0)
				Storage = new byte[capacity];
		}

		public override byte GetValueByte(ulong address)
		{
			if (address > (ulong)Capacity)
			{
				Interrupt = 1;
				return 0;
			}
			return Storage[address];
		}

		public override void SetValueByte(ulong address, byte value)
		{
			if (address > (ulong)Capacity)
			{
				Interrupt = 1;
				return;
			}
			Storage[address] = value;
		}

		public override ushort GetValueWord(ulong address)
		{
			if (address > (ulong)Capacity)
			{
				Interrupt = 1;
				return 0;
			}
			return (ushort) ((Storage[address + 0] << 8) +
			                 (Storage[address + 1] << 0));
		}

		public override void SetValueWord(ulong address, ushort value)
		{
			if (address > (ulong)Capacity)
			{
				Interrupt = 1;
				return;
			}
			Storage[address + 0] = (byte) ((value >> 8) & 0xFF);
			Storage[address + 1] = (byte) ((value >> 0) & 0xFF);
		}

		public override uint GetValueDWord(ulong address)
		{
			if (address > (ulong)Capacity)
			{
				Interrupt = 1;
				return 0;
			}
			
			return (uint) ((Storage[address + 0] << 24) +
			               (Storage[address + 1] << 16) +
			               (Storage[address + 2] << 8) +
			               (Storage[address + 3] << 0));
		}

		public override void SetValueDWord(ulong address, uint value)
		{
			if (address > (ulong)Capacity)
			{
				Interrupt = 1;
				return;
			}
			Storage[address + 0] = (byte) ((value >> 24) & 0xFF);
			Storage[address + 1] = (byte) ((value >> 16) & 0xFF);
			Storage[address + 2] = (byte) ((value >> 8) & 0xFF);
			Storage[address + 3] = (byte) ((value >> 0) & 0xFF);
		}

		public override ulong GetValueQWord(ulong address)
		{
			if (address > (ulong)Capacity)
			{
				Interrupt = 1;
				return 0;
			}
			return (ulong) ((Storage[address + 0] << 56) +
			                (Storage[address + 1] << 48) +
			                (Storage[address + 2] << 40) +
			                (Storage[address + 3] << 32) +
			                (Storage[address + 4] << 24) +
			                (Storage[address + 5] << 16) +
			                (Storage[address + 6] <<  8) +
			                (Storage[address + 7] <<  0));
		}

		public override void SetValueQWord(ulong address, ulong value)
		{
			if (address > (ulong)Capacity)
			{
				Interrupt = 1;
				return;
			}
			Storage[address + 0] = (byte) ((value >> 56) & 0xFF);
			Storage[address + 1] = (byte) ((value >> 48) & 0xFF);
			Storage[address + 2] = (byte) ((value >> 40) & 0xFF);
			Storage[address + 3] = (byte) ((value >> 32) & 0xFF);
			Storage[address + 4] = (byte) ((value >> 24) & 0xFF);
			Storage[address + 5] = (byte) ((value >> 16) & 0xFF);
			Storage[address + 6] = (byte) ((value >> 8) & 0xFF);
			Storage[address + 7] = (byte) ((value >> 0) & 0xFF);
		}
	}
}