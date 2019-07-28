#region Header
// https://gist.github.com/sharwell/9424666
/*
 * [The "BSD license"]
 *  Copyright (c) 2012 Terence Parr
 *  Copyright (c) 2012 Sam Harwell
 *  All rights reserved.
 *
 *  Redistribution and use in source and binary forms, with or without
 *  modification, are permitted provided that the following conditions
 *  are met:
 *
 *  1. Redistributions of source code must retain the above copyright
 *     notice, this list of conditions and the following disclaimer.
 *  2. Redistributions in binary form must reproduce the above copyright
 *     notice, this list of conditions and the following disclaimer in the
 *     documentation and/or other materials provided with the distribution.
 *  3. The name of the author may not be used to endorse or promote products
 *     derived from this software without specific prior written permission.
 *
 *  THIS SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY EXPRESS OR
 *  IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES
 *  OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED.
 *  IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT,
 *  INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT
 *  NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE,
 *  DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY
 *  THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 *  (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF
 *  THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */
#endregion
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Antlr4.Runtime;

namespace Strings.Tsql
{
	internal class AntlrCaseInsensitiveInputStream : AntlrInputStream
	{
		private readonly char[] lookaheadData;

		public AntlrCaseInsensitiveInputStream(string input) : base(input)
		{
			this.lookaheadData = LoadLookaheadData();
		}

		public AntlrCaseInsensitiveInputStream(TextReader reader) : base(reader)
		{
			this.lookaheadData = LoadLookaheadData();
		}

		private char[] LoadLookaheadData()
		{
			// TODO(jpg): this could maybe optimized
			return new string(this.data).ToUpperInvariant().ToCharArray();
		}

		public override int La(int i)
		{
			if (i == 0)
			{
				return 0; // undefined
			}
			if (i < 0)
			{
				i++; // e.g., translate LA(-1) to use offset i=0; then data[p+0-1]
				if ((this.p + i - 1) < 0)
				{
					return IntStreamConstants.Eof; // invalid; no char before first char
				}
			}

			if ((this.p + i - 1) >= this.n)
			{
				return IntStreamConstants.Eof;
			}

			return this.lookaheadData[this.p + i - 1];
		}
	}
}
