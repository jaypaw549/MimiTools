using System;
using System.Collections.Generic;
using System.Text;

namespace MimiTools.Data
{
    public interface IBlockEntity
    {
        /// <summary>
        /// The data provider associated with this entity
        /// </summary>
        DataProvider Provider { get; }

        /// <summary>
        /// The starting point of this entity
        /// </summary>
        long Start { get; }

        /// <summary>
        /// The length of this entity
        /// </summary>
        long Length { get; }

        /// <summary>
        /// Checks whether or not this entity is a valid
        /// </summary>
        /// <returns>true if the entity is valid</returns>
        bool CheckValid();
    }
}
