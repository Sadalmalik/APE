using System;
using System.Threading;

namespace Processor
{
	class Program
	{
		private static Memory        _memory;
		private static ProcessorCore _processorCore;

		private static byte[] _boot;

		static void Main(string[] args)
		{
			BuildBoot();

			_memory        = new Memory();
			_processorCore = new ProcessorCore();
			//	Память всегда будет девайсом номер 0
			_processorCore.Devices.Add(0, _memory);
			_processorCore.Devices.Add(4096, new ConstantMemory(_boot));

			//	На старте руками задаём нужное состояние процессора
			_processorCore.Registers.RDC.QWORD = 4096;
			_processorCore.Registers.RDR.QWORD = 4096;
			//	Адрес начала программы
			_processorCore.Registers.RIP.QWORD = 1024;

			//	Продолжаем пока не произойдёт обращение за границы оперативной памяти
			//	Пожалуй это не лучшее условие остановки, но пока и так сойдёт
			// int count = 0;
			Console.WriteLine("Start execution");
			Thread.Sleep(500);
			while (_memory.Interrupt == 0)
			{
				_processorCore.Tick();
				_processorCore.Registers.Dump();
				if (_processorCore.Completed)
					break;
				Thread.Sleep(100);
			}

			Console.WriteLine("Evaluation completed!");
			Console.WriteLine("Press any key to continue...");
			Console.ReadKey();
			Console.WriteLine("Press any key to exit...");
			Console.ReadKey();
		}

		private static void BuildBoot()
		{
			//	16kb
			_boot = new byte[1024 * 16];
			Assembler asm = new Assembler(_boot);
			Console.Write("\r\n");

			//	Итак, что я хочу?
			//	Я хочу запуститься с BOOT устройства где будет программа на ассемблере
			//	Эта программа должна взять строку в BOOT устройства и записать её в оперативную память
			//	Сначала накидаю общее описание
			//	
			//	memoryDevice:	.dword 0
			//	commandDevice:	.dword 4096
			//	messageSize:	.dword -размер строки-
			//	message:		.byte -строка-

			ulong memoryDevice;
			ulong commandDevice;
			ulong messageSize;
			ulong message;
			ulong loopStart;
			ulong loopEnd;

			asm.GetOffset(out memoryDevice);
			asm.AddValue(Size.DWord, 0);
			asm.GetOffset(out commandDevice);
			asm.AddValue(Size.DWord, 4096);
			asm.GetOffset(out messageSize);
			asm.AddString("Hello, world!");
			message = messageSize + 4;

			//	;	Дальше сама программа
			//	;	Берём переменные
			//  MOV [memoryDevice], RM0
			//	MOV [commandDevice], RM1
			//	MOV [messageSize], RM2
			//	MOV message, RM3

			asm.FillUntil(1024);
			asm.MOV_VARIABLE(Size.DWord, Asm.RM0, memoryDevice, true);
			asm.MOV_VARIABLE(Size.DWord, Asm.RM1, commandDevice, true);
			asm.MOV_VARIABLE(Size.DWord, Asm.RM2, messageSize, true);
			asm.MOV_ASSIGN(Size.DWord, Asm.RM3, false, message);

			    //	;	И в цикле копируем строку по одному байту
			    //	;	Сначала инициализируем сам цикл
			    //	MOV 0, RS0	; RS0 будет буфером
			    //	MOV 0, RS1	; RS1 счётчиком вывода
			asm.MOV_ASSIGN(Size.QWord, Asm.RS0, false, 0);
			asm.MOV_ASSIGN(Size.QWord, Asm.RS1, false, 0);

			    //	loop:
			    //	JZ RM2, endloop
			asm.GetOffset(out loopStart);
			asm.JumpCond(Size.Byte, Asm.RM2, 0, false);
			    //		;	Перенос из загрузчика в память
			    //		MOV RM1, RDR
			    //		MOV [RM3], RS0
			    //		MOV RM0, RDR
			    //		MOV RS0, [RS1]
			    //		MOV [RM1], RDR
			asm.MOV(Size.DWord, Asm.RM1, Asm.RDR, false, false);
			asm.MOV(Size.Byte, Asm.RM3, Asm.RS0, true, false);
			asm.MOV(Size.DWord, Asm.RM0, Asm.RDR, false, false);
			asm.MOV(Size.Byte, Asm.RS0, Asm.RS1, false, true);
			asm.MOV(Size.DWord, Asm.RM1, Asm.RDR, true, false);
			    //
			    //		;	Обработка счётчиков
			    //		INC RS1	;	Увеличиваем позицию записи
			    //		INC RM3	;	Увеличиваем позицию чтения
			    //		DEC RM2	;	Уменьшаем счётчик	
			asm.INC(Asm.RS1);
			asm.INC(Asm.RM3);
			asm.DEC(Asm.RM2);
			    //	Возврат в начало
			    //		JMP	loop
			asm.GetOffset(out loopEnd);
			
			sbyte delta = (sbyte)((sbyte)loopStart - (sbyte)loopEnd);
			asm.Jump(Size.Byte, Asm.JumpOffset(delta));
			    //	endloop:
			asm.GetOffset(out loopEnd);
			    //	INT	;	Прерывание работы процессора
			asm.INT();
			/* Done */

			//	Теперь после получения всех нужных координат - перезаписываем их
			asm.Offset = loopStart;
			delta = (sbyte)((sbyte)loopEnd - (sbyte)loopStart);
			asm.JumpCond(Size.Byte, Asm.RM2, Asm.JumpOffset(delta), false);
		}
	}
}