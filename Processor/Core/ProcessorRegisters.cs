using System;
using System.Linq;
using System.Text;

namespace Processor
{
	public class ProcessorRegisters
	{
		//	Регистры общего назначения (Register Main #)
		public readonly Register RM0 = new Register("RM0");
		public readonly Register RM1 = new Register("RM1");
		public readonly Register RM2 = new Register("RM2");
		public readonly Register RM3 = new Register("RM3");
		
		//	Регистры стеков (Register Stack #)
		public readonly Register RS0 = new Register("RS0");
		public readonly Register RS1 = new Register("RS1");
		public readonly Register RS2 = new Register("RS2");
		public readonly Register RS3 = new Register("RS3");
		
		//	Регистр адреса инструкции (Register Instruction Pointer)
		public readonly Register RIP = new Register("RIP");
		
		//	Регистр командного устройства (Register Command Device)
		public readonly Register RDC = new Register("RDC");
		
		//	Регистр устройства чтения/записи (Register Command Read/Write)
		public readonly Register RDR = new Register("RDR");
		
		//	Регистр флагов (Register Processor Flags)
		public readonly Register RPF = new Register("RPF");
		
		//	Регистры времени (счётчик тиков)
		public readonly Register RTX = new Register("RTX");
		
		//	Регистры защиты сегмента памяти (для защищённого режима)
		public readonly Register RLM = new Register("RLM"); //	Register Lower Memory
		public readonly Register RHM = new Register("RHM"); //	Register Hight Memory
		
		//	Все регистры для доступа по индексу
		public readonly Register[] registers;
		
		public Register this[int i] => registers[i];

		public ProcessorRegisters()
		{
			//	Менее 16 регистров - можно в командах адресовать их через 4 бита
			registers = new[]
			{
				RM0, RM1, RM2, RM3,
				RS0, RS1, RS2, RS3,
				RIP, RDC, RDR, RPF,
				RTX, RLM, RHM
			};
		}

		public void ResetAll()
		{
			foreach (var reg in registers)
				reg.QWORD = 0;
		}

		public void Dump()
		{
			Console.WriteLine(string.Join(", ", registers.Select(x=>$"{x.Name} = {x.QWORD:X}")));
			/*
			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < 15; i++)
			{
				var reg = registers[i];
				if (i > 0)
					sb.Append(i%4==0 ? "\n" : ", ");
				sb.Append($"{reg.Name} = {reg.QWORD:X}");
			}
			sb.Append("\n");
			Console.WriteLine(sb.ToString());
			//*/
		}
	}
}