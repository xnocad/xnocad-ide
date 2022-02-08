﻿namespace IDE.Core.Interfaces
{
	public interface IOutputView
	{
		void Clear();
		void ScrollToEnd();
		void AppendText(string text);
		void SetText(string text);
	}
}