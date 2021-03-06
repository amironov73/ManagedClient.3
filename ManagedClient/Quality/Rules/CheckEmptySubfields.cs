﻿// This is an open source non-commercial project. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++ and C#: http://www.viva64.com

/* CheckEmptySubfields.cs -- обнаружение пустых подполей
 */

#region Using directives

using JetBrains.Annotations;

using MoonSharp.Interpreter;

#endregion

namespace ManagedClient.Quality.Rules
{
    /// <summary>
    /// Обнаружение пустых подполей
    /// </summary>
    [PublicAPI]
    [MoonSharpUserData]
    public sealed class CheckEmptySubfields
        : IrbisRule
    {
        #region Private members

        private void _CheckField
            (
                [NotNull] RecordField field
            )
        {
            foreach (SubField subField in field.SubFields)
            {
                if (string.IsNullOrEmpty(subField.Text))
                {
                    AddDefect
                        (
                            field,
                            subField,
                            3,
                            "Пустое подполе {0}^{1}",
                            field.Tag,
                            subField.Code
                        );
                }
            }
        }

        #endregion

        #region IrbisRule members

        /// <inheritdoc cref="IrbisRule.FieldSpec"/>
        public override string FieldSpec
        {
            get { return "!100,330,905,907,919,920,3005"; }
        }

        /// <inheritdoc cref="IrbisRule.CheckRecord"/>
        public override RuleReport CheckRecord
            (
                RuleContext context
            )
        {
            BeginCheck(context);

            RecordField[] fields = GetFields();
            foreach (RecordField field in fields)
            {
                _CheckField
                    (
                        field
                    );
            }

            return EndCheck();
        }

        #endregion
    }
}
