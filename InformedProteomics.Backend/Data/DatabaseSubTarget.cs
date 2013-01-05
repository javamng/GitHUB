﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DeconTools.Backend;
using DeconTools.Backend.Core;

namespace InformedProteomics.Backend.Data
{
	public class DatabaseSubTarget : TargetBase
	{
		public DatabaseSubTarget(String sequence, String empiricalFormula, double mass, double mz, short chargeState, float elutionTime) : base()
		{
			this.Code = sequence;
			this.EmpiricalFormula = empiricalFormula;
			this.MonoIsotopicMass = mass;
			this.MZ = mz;
			this.ChargeState = chargeState;
			ElutionTimeUnit = Globals.ElutionTimeUnit.NormalizedElutionTime;
			this.NormalizedElutionTime = elutionTime;
			this.MsLevel = 1;
		}

		/// <summary>
		/// Returns the empirical formula. It was necessary to override this from DeconTools. Our version just returns the stored formula instead of calculating a new one.
		/// </summary>
		/// <returns></returns>
		public override string GetEmpiricalFormulaFromTargetCode()
		{
			return this.EmpiricalFormula;
		}

		public override string ToString()
		{
			return string.Format("ID: {0}, MonoIsotopicMass: {1}, ChargeState: {2}, MZ: {3}, Code: {4}, MsLevel: {5}, NormalizedElutionTime: {6}", ID, MonoIsotopicMass, ChargeState, MZ, Code, MsLevel, NormalizedElutionTime);
		}
	}
}
