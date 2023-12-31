﻿namespace IDE.Core.Interfaces
{
	using System;
	using System.Collections.Generic;

	public enum TypeOfResult
	{
		FileLoad
	}

	/// <summary>
	/// Stores information about the mResult of a batch run.
	/// If an error occurs, Error is set to true and an exception may be stored in InnerException.
	/// </summary>
	public class ProcessResultEvent : EventArgs
	{
		#region Fields

		readonly bool cancel;
		readonly bool error;
		readonly TypeOfResult typeOfResult;
		readonly Exception innerException;
		readonly string message;
		readonly Dictionary<string, object> objColl;

		#endregion Fields

		#region Constructors
		/// <summary>
		/// Convinience constructor to produce simple message for communicating when
		/// batch run was abborted incompletely (bCancel can be set to true or bError
		/// can be set to true).
		/// </summary>
		/// <param name="sMessage"></param>
		/// <param name="bError"></param>
		/// <param name="bCancel"></param>
		/// <param name="objColl"></param>
		/// <param name="innerException"></param>
		public ProcessResultEvent(string sMessage,
								  bool bError,
								  bool bCancel,
								  TypeOfResult processResult,
								  Dictionary<string, object> objColl = null,
								  Exception innerException = null)
		{
			message = sMessage;
			error = bError;
			typeOfResult = processResult;
			cancel = bCancel;
			this.innerException = innerException;
			this.objColl = (objColl == null ? null : new Dictionary<string, object>(objColl));
		}

		/// <summary>
		/// Convinience constructor to produce simple message at the end of a batch run
		/// (Cancel not clicked and no error to be reported).
		/// </summary>
		/// <param name="message"></param>
		public ProcessResultEvent(string message)
		{
			this.message = message;
			error = false;
			cancel = false;
			innerException = null;
		}
		#endregion Constructors

		#region Properties
		/// <summary>
		/// Get an error message if processing task aborted with errors
		/// </summary>
		public bool Error
		{
			get { return error; }
		}

		/// <summary>
		/// Get property to determine whether processing was cancelled or not.
		/// </summary>
		public bool Cancel
		{
			get { return cancel; }
		}

		public TypeOfResult TypeOfResult
		{
			get { return typeOfResult; }
		}

		/// <summary>
		/// Get property to determine whether there is an innerException to
		/// document an abortion with errors.
		/// </summary>
		public Exception InnerException
		{
			get { return innerException; }
		}

		/// <summary>
		/// Get current message describing the current step being proceesed
		/// 
		/// </summary>
		public string Message
		{
			get { return message; }
		}

		/// <summary>
		/// Dictionary of mResult objects from executing process
		/// </summary>
		public Dictionary<string, object> ResultObjects
		{
			get
			{
				return (this.ObjColl == null
										? new Dictionary<string, object>()
										: new Dictionary<string, object>(ObjColl));
			}
		}

        public Dictionary<string, object> ObjColl
        {
            get
            {
                return objColl;
            }
        }
        #endregion Properties
    }
}
