﻿using System;
using System.Collections.Generic;
using System.Text;

namespace LTE.InternalInterference
{
    class APX5TOGain:AbstrGain
    {
        public override double[] GetHAGain()
        {
            HAGain[0] = 0;
            HAGain[1] = 0;
            HAGain[2] = 0;
            HAGain[3] = 0;
            HAGain[4] = 0;
            HAGain[5] = 0;
            HAGain[6] = 0;
            HAGain[7] = 0.1;
            HAGain[8] = 0.1;
            HAGain[9] = 0.1;
            HAGain[10] = 0.1;
            HAGain[11] = 0.2;
            HAGain[12] = 0.2;
            HAGain[13] = 0.2;
            HAGain[14] = 0.3;
            HAGain[15] = 0.3;
            HAGain[16] = 0.4;
            HAGain[17] = 0.4;
            HAGain[18] = 0.4;
            HAGain[19] = 0.5;
            HAGain[20] = 0.6;
            HAGain[21] = 0.6;
            HAGain[22] = 0.7;
            HAGain[23] = 0.7;
            HAGain[24] = 0.8;
            HAGain[25] = 0.9;
            HAGain[26] = 0.9;
            HAGain[27] = 1;
            HAGain[28] = 1.1;
            HAGain[29] = 1.2;
            HAGain[30] = 1.2;
            HAGain[31] = 1.3;
            HAGain[32] = 1.4;
            HAGain[33] = 1.5;
            HAGain[34] = 1.6;
            HAGain[35] = 1.7;
            HAGain[36] = 1.8;
            HAGain[37] = 1.9;
            HAGain[38] = 2;
            HAGain[39] = 2.1;
            HAGain[40] = 2.2;
            HAGain[41] = 2.3;
            HAGain[42] = 2.4;
            HAGain[43] = 2.5;
            HAGain[44] = 2.6;
            HAGain[45] = 2.7;
            HAGain[46] = 2.8;
            HAGain[47] = 3;
            HAGain[48] = 3.1;
            HAGain[49] = 3.2;
            HAGain[50] = 3.3;
            HAGain[51] = 3.5;
            HAGain[52] = 3.6;
            HAGain[53] = 3.7;
            HAGain[54] = 3.8;
            HAGain[55] = 4;
            HAGain[56] = 4.1;
            HAGain[57] =4.2;
            HAGain[58] = 4.4;
            HAGain[59] = 4.5;
            HAGain[60] = 4.6;
            HAGain[61] = 4.8;
            HAGain[62] = 4.9;
            HAGain[63] = 5.1;
            HAGain[64] = 5.2;
            HAGain[65] = 5.4;
            HAGain[66] = 5.5;
            HAGain[67] = 5.7;
            HAGain[68] = 5.8;
            HAGain[69] = 6;
            HAGain[70] = 6.1;
            HAGain[71] = 6.3;
            HAGain[72] = 6.4;
            HAGain[73] = 6.6;
            HAGain[74] = 6.7;
            HAGain[75] = 6.9;
            HAGain[76] = 7;
            HAGain[77] = 7.2;
            HAGain[78] = 7.4;
            HAGain[79] = 7.5;
            HAGain[80] = 7.7;
            HAGain[81] = 7.9;
            HAGain[82] = 8;
            HAGain[83] = 8.2;
            HAGain[84] = 8.4;
            HAGain[85] = 8.6;
            HAGain[86] = 8.7;
            HAGain[87] = 8.9;
            HAGain[88] = 9.1;
            HAGain[89] = 9.3;
            HAGain[90] = 9.4;
            HAGain[91] = 9.6;
            HAGain[92] = 9.8;
            HAGain[93] = 10;
            HAGain[94] = 10.2;
            HAGain[95] = 10.4;
            HAGain[96] = 10.6;
            HAGain[97] = 10.7;
            HAGain[98] = 10.9;
            HAGain[99] = 11.1;
            HAGain[100] = 11.3;
            HAGain[101] = 11.5;
            HAGain[102] = 11.7;
            HAGain[103] = 11.9;
            HAGain[104] = 12.1;
            HAGain[105] = 12.2;
            HAGain[106] = 12.4;
            HAGain[107] = 12.7;
            HAGain[108] = 12.8;
            HAGain[109] = 13.1;
            HAGain[110] =13.3;
            HAGain[111] = 13.4;
            HAGain[112] = 13.6;
            HAGain[113] = 13.8;
            HAGain[114] = 14;
            HAGain[115] = 14.2;
            HAGain[116] = 14.4;
            HAGain[117] = 14.6;
            HAGain[118] = 14.8;
            HAGain[119] = 15;
            HAGain[120] = 15.2;
            HAGain[121] = 15.4;
            HAGain[122] = 15.6;
            HAGain[123] = 15.8;
            HAGain[124] = 16;
            HAGain[125] = 16.2;
            HAGain[126] = 16.4;
            HAGain[127] = 16.6;
            HAGain[128] = 16.7;
            HAGain[129] = 16.9;
            HAGain[130] = 17.1;
            HAGain[131] = 17.3;
            HAGain[132] = 17.5;
            HAGain[133] = 17.6;
            HAGain[134] = 17.8;
            HAGain[135] = 18;
            HAGain[136] = 18.1;
            HAGain[137] = 18.3;
            HAGain[138] = 18.4;
            HAGain[139] = 18.8;
            HAGain[140] = 18.7;
            HAGain[141] = 18.8;
            HAGain[142] = 18.9;
            HAGain[143] = 19.4;
            HAGain[144] = 19.4;
            HAGain[145] = 19.5;
            HAGain[146] = 19.6;
            HAGain[147] = 19.7;
            HAGain[148] = 19.8;
            HAGain[149] = 19.9;
            HAGain[150] = 19.9;
            HAGain[151] = 20;
            HAGain[152] = 20;
            HAGain[153] = 20.1;
            HAGain[154] = 20.2;
            HAGain[155] = 20.2;
            HAGain[156] = 20.2;
            HAGain[157] = 20.3;
            HAGain[158] = 20.2;
            HAGain[159] = 20.3;
            HAGain[160] = 20.3;
            HAGain[161] = 20.3;
            HAGain[162] = 20.3;
            HAGain[163] = 20.3;
            HAGain[164] = 20.3;
            HAGain[165] = 20.4;
            HAGain[166] = 20.4;
            HAGain[167] = 20.4;
            HAGain[168] = 20.3;
            HAGain[169] = 20.4;
            HAGain[170] = 20.3;
            HAGain[171] = 20.4;
            HAGain[172] = 20.4;
            HAGain[173] = 20.4;
            HAGain[174] = 20.4;
            HAGain[175] = 20.3;
            HAGain[176] = 20.4;
            HAGain[177] = 20.4;
            HAGain[178] = 20.4;
            HAGain[179] = 20.4;
            HAGain[180] = 20.4;
            HAGain[181] = 20.5;
            HAGain[182] = 20.5;
            HAGain[183] = 20.6;
            HAGain[184] = 20.6;
            HAGain[185] = 20.6;
            HAGain[186] = 20.7;
            HAGain[187] = 20.7;
            HAGain[188] = 20.7;
            HAGain[189] = 20.8;
            HAGain[190] = 20.8;
            HAGain[191] = 20.8;
            HAGain[192] = 20.9;
            HAGain[193] = 20.9;
            HAGain[194] = 20.9;
            HAGain[195] = 21;
            HAGain[196] = 21;
            HAGain[197] = 20.9;
            HAGain[198] = 21;
            HAGain[199] = 21;
            HAGain[200] =21;
            HAGain[201] = 21;
            HAGain[202] = 20.9;
            HAGain[203] = 20.9;
            HAGain[204] = 20.9;
            HAGain[205] = 20.9;
            HAGain[206] = 20.8;
            HAGain[207] = 20.7;
            HAGain[208] = 20.6;
            HAGain[209] = 20.5;
            HAGain[210] = 20.4;
            HAGain[211] = 20.3;
            HAGain[212] = 20.2;
            HAGain[213] = 20.1;
            HAGain[214] = 19.9;
            HAGain[215] = 19.8;
            HAGain[216] = 19.6;
            HAGain[217] = 19.4;
            HAGain[218] = 19.3;
            HAGain[219] = 18.8;
            HAGain[220] = 18.9;
            HAGain[221] = 18.4;
            HAGain[222] = 18.3;
            HAGain[223] = 18.1;
            HAGain[224] = 17.9;
            HAGain[225] = 17.7;
            HAGain[226] = 17.5;
            HAGain[227] = 17.2;
            HAGain[228] = 17.1;
            HAGain[229] = 16.8;
            HAGain[230] = 16.6;
            HAGain[231] = 16.4;
            HAGain[232] = 16.2;
            HAGain[233] = 16.1;
            HAGain[234] = 15.8;
            HAGain[235] = 15.6;
            HAGain[236] = 15.4;
            HAGain[237] = 15.2;
            HAGain[238] = 15;
            HAGain[239] = 14.8;
            HAGain[240] = 14.6;
            HAGain[241] = 14.4;
            HAGain[242] = 14.2;
            HAGain[243] = 14;
            HAGain[244] = 13.8;
            HAGain[245] = 13.6;
            HAGain[246] = 13.4;
            HAGain[247] = 13.3;
            HAGain[248] = 13.1;
            HAGain[249] = 12.9;
            HAGain[250] = 12.7;
            HAGain[251] = 12.5;
            HAGain[252] = 12.3;
            HAGain[253] = 12.1;
            HAGain[254] = 12;
            HAGain[255] = 11.8;
            HAGain[256] = 11.6;
            HAGain[257] = 11.5;
            HAGain[258] = 11.3;
            HAGain[259] = 11.1;
            HAGain[260] = 10.9;
            HAGain[261] = 10.8;
            HAGain[262] = 10.6;
            HAGain[263] = 10.4;
            HAGain[264] = 10.2;
            HAGain[265] = 10.1;
            HAGain[266] = 9.9;
            HAGain[267] = 9.7;
            HAGain[268] = 9.6;
            HAGain[269] = 9.4;
            HAGain[270] = 9.2;
            HAGain[271] = 9;
            HAGain[272] = 8.9;
            HAGain[273] = 8.7;
            HAGain[274] = 8.6;
            HAGain[275] = 8.4;
            HAGain[276] = 8.2;
            HAGain[277] = 8.1;
            HAGain[278] = 7.9;
            HAGain[279] = 7.8;
            HAGain[280] = 7.6;
            HAGain[281] = 7.4;
            HAGain[282] = 7.3;
            HAGain[283] = 7.1;
            HAGain[284] = 7;
            HAGain[285] = 6.8;
            HAGain[286] = 6.7;
            HAGain[287] = 6.5;
            HAGain[288] = 6.3;
            HAGain[289] = 6.2;
            HAGain[290] = 6.1;
            HAGain[291] = 5.9;
            HAGain[292] = 5.8;
            HAGain[293] = 5.6;
            HAGain[294] = 5.5;
            HAGain[295] = 5.3;
            HAGain[296] = 5.2;
            HAGain[297] = 5;
            HAGain[298] = 4.9;
            HAGain[299] = 4.8;
            HAGain[300] = 4.6;
            HAGain[301] = 4.5;
            HAGain[302] = 4.4;
            HAGain[303] = 4.2;
            HAGain[304] = 4.1;
            HAGain[305] = 4;
            HAGain[306] = 3.8;
            HAGain[307] = 3.7;
            HAGain[308] = 3.6;
            HAGain[309] = 3.4;
            HAGain[310] = 3.3;
            HAGain[311] = 3.2;
            HAGain[312] = 3.1;
            HAGain[313] = 3;
            HAGain[314] = 2.9;
            HAGain[315] = 2.7;
            HAGain[316] = 2.6;
            HAGain[317] = 2.5;
            HAGain[318] = 2.4;
            HAGain[319] = 2.3;
            HAGain[320] = 2.2;
            HAGain[321] = 2.1;
            HAGain[322] = 2;
            HAGain[323] = 1.9;
            HAGain[324] = 1.8;
            HAGain[325] = 1.7;
            HAGain[326] = 1.6;
            HAGain[327] = 1.5;
            HAGain[328] = 1.4;
            HAGain[329] = 1.3;
            HAGain[330] = 1.2;
            HAGain[331] = 1.2;
            HAGain[332] = 1.1;
            HAGain[333] = 1;
            HAGain[334] = 0.9;
            HAGain[335] = 0.9;
            HAGain[336] = 0.8;
            HAGain[337] = 0.8;
            HAGain[338] = 0.7;
            HAGain[339] = 0.6;
            HAGain[340] = 0.6;
            HAGain[341] = 0.5;
            HAGain[342] = 0.5;
            HAGain[343] = 0.4;
            HAGain[344] = 0.4;
            HAGain[345] = 0.3;
            HAGain[346] = 0.3;
            HAGain[347] = 0.2;
            HAGain[348] = 0.2;
            HAGain[349] = 0.2;
            HAGain[350] = 0.2;
            HAGain[351] = 0.1;
            HAGain[352] = 0.1;
            HAGain[353] = 0.1;
            HAGain[354] = 0.1;
            HAGain[355] = 0.1;
            HAGain[356] = 0;
            HAGain[357] = 0;
            HAGain[358] = 0.0;
            HAGain[359] = 0.0;

            return HAGain;
        }
        public override double[] GetVAGain()
        {
            VAGain[0] = 0;
            VAGain[1] = 2;
            VAGain[2] = 1;
            VAGain[3] = 0.4;
            VAGain[4] = 0.1;
            VAGain[5] = 0;
            VAGain[6] = 0.3;
            VAGain[7] = 0.8;
            VAGain[8] = 1.7;
            VAGain[9] = 3;
            VAGain[10] = 4.6;
            VAGain[11] = 6.8;
            VAGain[12] = 9.8;
            VAGain[13] = 13.7;
            VAGain[14] = 18.9;
            VAGain[15] = 18.8;
            VAGain[16] = 15.4;
            VAGain[17] = 14.1;
            VAGain[18] = 12.4;
            VAGain[19] = 11.5;
            VAGain[20] = 11.3;
            VAGain[21] = 11.8;
            VAGain[22] = 12.8;
            VAGain[23] = 14.3;
            VAGain[24] = 16.3;
            VAGain[25] = 19.4;
            VAGain[26] = 22.2;
            VAGain[27] = 23.4;
            VAGain[28] = 22.2;
            VAGain[29] = 20.8;
            VAGain[30] = 19.9;
            VAGain[31] = 19.7;
            VAGain[32] = 20.1;
            VAGain[33] = 21.3;
            VAGain[34] = 23.7;
            VAGain[35] = 28.3;
            VAGain[36] = 38;
            VAGain[37] = 35.4;
            VAGain[38] = 26.6;
            VAGain[39] = 22.7;
            VAGain[40] = 20.3;
            VAGain[41] = 18.7;
            VAGain[42] = 17.3;
            VAGain[43] = 16.6;
            VAGain[44] = 16.3;
            VAGain[45] = 16.2;
            VAGain[46] = 16.4;
            VAGain[47] = 16.7;
            VAGain[48] = 17.3;
            VAGain[49] = 18;
            VAGain[50] = 19.2;
            VAGain[51] = 20.2;
            VAGain[52] = 21.4;
            VAGain[53] = 22.6;
            VAGain[54] = 24.1;
            VAGain[55] = 25.4;
            VAGain[56] = 26.5;
            VAGain[57] = 27.7;
            VAGain[58] = 28.8;
            VAGain[59] = 29.8;
            VAGain[60] = 30.7;
            VAGain[61] = 31.6;
            VAGain[62] = 32.5;
            VAGain[63] = 33.7;
            VAGain[64] = 34.4;
            VAGain[65] = 35.1;
            VAGain[66] = 35.4;
            VAGain[67] = 35.1;
            VAGain[68] = 34.6;
            VAGain[69] = 33.5;
            VAGain[70] = 32.4;
            VAGain[71] = 31.7;
            VAGain[72] = 30.8;
            VAGain[73] = 30.2;
            VAGain[74] = 29.6;
            VAGain[75] = 29.2;
            VAGain[76] = 28.9;
            VAGain[77] = 28.5;
            VAGain[78] = 28.2;
            VAGain[79] = 28.1;
            VAGain[80] = 28;
            VAGain[81] = 28;
            VAGain[82] = 28;
            VAGain[83] = 28.2;
            VAGain[84] = 28.4;
            VAGain[85] = 28.5;
            VAGain[86] = 28.9;
            VAGain[87] = 29.2;
            VAGain[88] = 29.5;
            VAGain[89] = 29.9;
            VAGain[90] = 30.5;
            VAGain[91] = 31;
            VAGain[92] = 31.5;
            VAGain[93] = 32.1;
            VAGain[94] = 32.8;
            VAGain[95] = 33.6;
            VAGain[96] = 34.5;
            VAGain[97] = 35.3;
            VAGain[98] = 36.4;
            VAGain[99] = 37.4;
            VAGain[100] = 38.7;
            VAGain[101] = 40.4;
            VAGain[102] = 40.8;
            VAGain[103] = 41.6;
            VAGain[104] = 41.7;
            VAGain[105] = 40.8;
            VAGain[106] = 39.9;
            VAGain[107] = 38.5;
            VAGain[108] = 37.6;
            VAGain[109] = 36.6;
            VAGain[110] = 35.8;
            VAGain[111] = 35;
            VAGain[112] = 34.2;
            VAGain[113] = 33.8;
            VAGain[114] = 33.3;
            VAGain[115] = 33;
            VAGain[116] = 32.7;
            VAGain[117] = 32.4;
            VAGain[118] = 32.3;
            VAGain[119] = 32.3;
            VAGain[120] = 32.4;
            VAGain[121] = 32.6;
            VAGain[122] = 32.7;
            VAGain[123] = 33;
            VAGain[124] = 33.1;
            VAGain[125] = 33.2;
            VAGain[126] = 33.2;
            VAGain[127] = 33;
            VAGain[128] = 33;
            VAGain[129] = 32.6;
            VAGain[130] = 32.5;
            VAGain[131] = 32.3;
            VAGain[132] = 31.9;
            VAGain[133] = 31.6;
            VAGain[134] = 31.5;
            VAGain[135] = 31.1;
            VAGain[136] = 30.9;
            VAGain[137] = 30.8;
            VAGain[138] = 30.7;
            VAGain[139] = 30.8;
            VAGain[140] = 31;
            VAGain[141] = 31.3;
            VAGain[142] = 32;
            VAGain[143] = 32.7;
            VAGain[144] = 34;
            VAGain[145] = 35.3;
            VAGain[146] = 37.2;
            VAGain[147] = 38.6;
            VAGain[148] = 39.8;
            VAGain[149] = 40.5;
            VAGain[150] = 40.5;
            VAGain[151] = 40.5;
            VAGain[152] = 41.8;
            VAGain[153] = 41.9;
            VAGain[154] = 41.5;
            VAGain[155] = 40.6;
            VAGain[156] = 38;
            VAGain[157] = 35.8;
            VAGain[158] = 33.8;
            VAGain[159] = 32.2;
            VAGain[160] = 31.3;
            VAGain[161] = 30.9;
            VAGain[162] = 31.1;
            VAGain[163] = 32.1;
            VAGain[164] = 34.1;
            VAGain[165] = 38.7;
            VAGain[166] = 51.4;
            VAGain[167] = 39;
            VAGain[168] = 32.5;
            VAGain[169] = 28.7;
            VAGain[170] = 25.9;
            VAGain[171] = 23.8;
            VAGain[172] = 22.5;
            VAGain[173] = 21.5;
            VAGain[174] = 20.8;
            VAGain[175] = 20.4;
            VAGain[176] = 20.2;
            VAGain[177] = 20.4;
            VAGain[178] = 20.7;
            VAGain[179] = 21.3;
            VAGain[180] = 22.2;
            VAGain[181] = 23.4;
            VAGain[182] = 25;
            VAGain[183] = 27.1;
            VAGain[184] = 29.6;
            VAGain[185] = 35.5;
            VAGain[186] = 40.9;
            VAGain[187] = 45.8;
            VAGain[188] = 41.4;
            VAGain[189] = 38.5;
            VAGain[190] = 37.5;
            VAGain[191] = 37.5;
            VAGain[192] = 38.4;
            VAGain[193] = 40.3;
            VAGain[194] = 43.5;
            VAGain[195] = 47.6;
            VAGain[196] = 57;
            VAGain[197] = 54.8;
            VAGain[198] = 48.2;
            VAGain[199] = 44.8;
            VAGain[200] = 43.3;
            VAGain[201] = 42.7;
            VAGain[202] = 43.3;
            VAGain[203] = 44.9;
            VAGain[204] = 46.4;
            VAGain[205] = 49;
            VAGain[206] = 48.7;
            VAGain[207] = 46.7;
            VAGain[208] = 43.8;
            VAGain[209] = 41.3;
            VAGain[210] = 39.5;
            VAGain[211] = 38.1;
            VAGain[212] = 36.9;
            VAGain[213] = 36.4;
            VAGain[214] = 35.8;
            VAGain[215] = 35.6;
            VAGain[216] = 35.3;
            VAGain[217] = 35.1;
            VAGain[218] = 35.1;
            VAGain[219] = 34.9;
            VAGain[220] = 34.9;
            VAGain[221] = 34.6;
            VAGain[222] = 34.4;
            VAGain[223] = 34.3;
            VAGain[224] = 34.3;
            VAGain[225] = 34.4;
            VAGain[226] = 34.4;
            VAGain[227] = 34.6;
            VAGain[228] = 35;
            VAGain[229] = 35.5;
            VAGain[230] = 36.4;
            VAGain[231] = 37.3;
            VAGain[232] = 38.9;
            VAGain[233] = 41.5;
            VAGain[234] = 44.3;
            VAGain[235] = 48.1;
            VAGain[236] = 54.3;
            VAGain[237] = 54.4;
            VAGain[238] = 49.2;
            VAGain[239] = 45.3;
            VAGain[240] = 42.8;
            VAGain[241] = 40.9;
            VAGain[242] = 39.6;
            VAGain[243] = 38.8;
            VAGain[244] = 38.2;
            VAGain[245] = 37.9;
            VAGain[246] = 37.8;
            VAGain[247] = 37.8;
            VAGain[248] = 38.1;
            VAGain[249] = 38.7;
            VAGain[250] = 38.8;
            VAGain[251] = 39.5;
            VAGain[252] = 40.3;
            VAGain[253] = 42.2;
            VAGain[254] = 44.2;
            VAGain[255] = 47;
            VAGain[256] = 52.4;
            VAGain[257] = 60.5;
            VAGain[258] = 49.9;
            VAGain[259] = 45.3;
            VAGain[260] = 41.3;
            VAGain[261] = 38.8;
            VAGain[262] = 36.5;
            VAGain[263] = 35;
            VAGain[264] = 33.8;
            VAGain[265] = 32.3;
            VAGain[266] = 31.3;
            VAGain[267] = 30.5;
            VAGain[268] = 29.8;
            VAGain[269] = 28.8;
            VAGain[270] = 28.1;
            VAGain[271] = 27.6;
            VAGain[272] = 27;
            VAGain[273] = 26.3;
            VAGain[274] = 25.8;
            VAGain[275] = 25.3;
            VAGain[276] = 24.8;
            VAGain[277] = 24.4;
            VAGain[278] = 23.9;
            VAGain[279] = 23.7;
            VAGain[280] = 23.5;
            VAGain[281] = 23.4;
            VAGain[282] = 23.3;
            VAGain[283] = 23.3;
            VAGain[284] = 23.4;
            VAGain[285] = 23.6;
            VAGain[286] = 23.9;
            VAGain[287] = 24.3;
            VAGain[288] = 24.8;
            VAGain[289] = 25.4;
            VAGain[290] = 26.2;
            VAGain[291] = 27.1;
            VAGain[292] = 28.2;
            VAGain[293] = 29.5;
            VAGain[294] = 30.8;
            VAGain[295] = 31.8;
            VAGain[296] = 32.1;
            VAGain[297] = 31.5;
            VAGain[298] = 30.4;
            VAGain[299] = 29;
            VAGain[300] = 27.9;
            VAGain[301] = 27;
            VAGain[302] = 26.3;
            VAGain[303] = 26;
            VAGain[304] = 26;
            VAGain[305] = 26.5;
            VAGain[306] = 27.4;
            VAGain[307] = 29;
            VAGain[308] = 32.1;
            VAGain[309] = 37.6;
            VAGain[310] = 56.9;
            VAGain[311] = 35.2;
            VAGain[312] = 29;
            VAGain[313] = 25.4;
            VAGain[314] = 23;
            VAGain[315] = 21.2;
            VAGain[316] = 19.9;
            VAGain[317] = 18.9;
            VAGain[318] = 18.1;
            VAGain[319] = 17.8;
            VAGain[320] = 17.8;
            VAGain[321] = 18.5;
            VAGain[322] = 19.1;
            VAGain[323] = 20;
            VAGain[324] = 21.1;
            VAGain[325] = 22.2;
            VAGain[326] = 23;
            VAGain[327] = 23.2;
            VAGain[328] = 23;
            VAGain[329] = 22.8;
            VAGain[330] = 22.8;
            VAGain[331] = 23.4;
            VAGain[332] = 24.9;
            VAGain[333] = 27.4;
            VAGain[334] = 31.4;
            VAGain[335] = 31.7;
            VAGain[336] = 26.7;
            VAGain[337] = 22.7;
            VAGain[338] = 20.2;
            VAGain[339] = 18.5;
            VAGain[340] = 17.3;
            VAGain[341] = 17.1;
            VAGain[342] = 17.5;
            VAGain[343] =19;
            VAGain[344] = 21.4;
            VAGain[345] = 24.6;
            VAGain[346] = 26.5;
            VAGain[347] = 23.2;
            VAGain[348] = 19.8;
            VAGain[349] = 17.4;
            VAGain[350] = 16.3;
            VAGain[351] = 16.2;
            VAGain[352] = 17.4;
            VAGain[353] = 20.8;
            VAGain[354] = 29.1;
            VAGain[355] = 23.1;
            VAGain[356] = 15.2;
            VAGain[357] = 10.5;
            VAGain[358] = 7.4;
            VAGain[359] = 5;

            return VAGain;
        }
    }
}
