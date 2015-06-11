namespace Microsoft.DesignPatterns.Examples
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Represents the base implementation for a unit of work.
    /// </summary>
    /// <typeparam name="T">The <see cref="Type">type</see> of items the unit of work contains.</typeparam>
    [ContractClass( typeof( UnitOfWorkContract<> ) )]
    public abstract class UnitOfWork<T> : IUnitOfWork<T> where T : class
    {
        private readonly IEqualityComparer<T> comparer;
        private readonly HashSet<T> inserted;
        private readonly HashSet<T> updated;
        private readonly HashSet<T> deleted;

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitOfWork{T}"/> class.
        /// </summary>
        protected UnitOfWork()
            : this( EqualityComparer<T>.Default )
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnitOfWork{T}"/> class.
        /// </summary>
        /// <param name="comparer">The <see cref="IEqualityComparer{T}">comparer</see> used to comparer item equality.</param>
        protected UnitOfWork( IEqualityComparer<T> comparer )
        {
            Contract.Requires( comparer != null, "comparer" );

            this.comparer = comparer;
            this.inserted = new HashSet<T>( comparer );
            this.updated = new HashSet<T>( comparer );
            this.deleted = new HashSet<T>( comparer );
        }

        /// <summary>
        /// Gets the equality comparer used by the unit of work.
        /// </summary>
        /// <value>An <see cref="IEqualityComparer{T}"/> object.</value>
        protected IEqualityComparer<T> Comparer
        {
            get
            {
                Contract.Ensures( this.comparer != null );
                return this.comparer;
            }
        }

        /// <summary>
        /// Gets a collection of the inserted units of work.
        /// </summary>
        /// <value>A <see cref="ICollection{T}">collection</see> of inserted items.</value>
        protected virtual ICollection<T> InsertedItems
        {
            get
            {
                Contract.Ensures( Contract.Result<ICollection<T>>() != null );
                return this.inserted;
            }
        }

        /// <summary>
        /// Gets a collection of the updated units of work.
        /// </summary>
        /// <value>A <see cref="ICollection{T}">collection</see> of updated items.</value>
        protected virtual ICollection<T> UpdatedItems
        {
            get
            {
                Contract.Ensures( Contract.Result<ICollection<T>>() != null );
                return this.updated;
            }
        }

        /// <summary>
        /// Gets a collection of the deleted units of work.
        /// </summary>
        /// <value>A <see cref="ICollection{T}">collection</see> of deleted items.</value>
        protected virtual ICollection<T> DeletedItems
        {
            get
            {
                Contract.Ensures( Contract.Result<ICollection<T>>() != null );
                return this.deleted;
            }
        }

        /// <summary>
        /// Raises the <see cref="E:PropertyChanged"/> event for the supplied property name.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed. The default value is the name of the
        /// member the method is invoked from<seealso cref="CallerMemberNameAttribute"/>.</param>
        protected void OnPropertyChanged( string propertyName )
        {
            this.OnPropertyChanged( new PropertyChangedEventArgs( propertyName ) );
        }

        /// <summary>
        /// Raises the <see cref="E:PropertyChanged"/> event.
        /// </summary>
        /// <param name="e">The <see cref="PropertyChangedEventArgs"/> event data.</param>
        protected virtual void OnPropertyChanged( PropertyChangedEventArgs e )
        {
            Contract.Requires( e != null, "e" );

            var handler = this.PropertyChanged;

            if ( handler != null )
                handler( this, e );
        }

        /// <summary>
        /// Accepts all of the pending changes in the unit of work.
        /// </summary>
        protected virtual void AcceptChanges()
        {
            var hasChanges = this.HasPendingChanges;

            this.InsertedItems.Clear();
            this.UpdatedItems.Clear();
            this.DeletedItems.Clear();

            if ( this.HasPendingChanges != hasChanges )
                this.OnPropertyChanged( "HasPendingChanges" );
        }

        /// <summary>
        /// Returns a value indicating whether the specified item is a new item.
        /// </summary>
        /// <param name="item">The <typeparamref name="T">item</typeparamref> to evaluate.</param>
        /// <returns>True if the item is a new item; otherwise, false.</returns>
        protected abstract bool IsNew( T item );

        /// <summary>
        /// Gets a value indicating whether there is an pending, uncommitted changes.
        /// </summary>
        /// <value>True if there are any pending uncommitted changes; otherwise, false.</value>
        public virtual bool HasPendingChanges
        {
            get
            {
                return this.InsertedItems.Any() || this.UpdatedItems.Any() || this.DeletedItems.Any();
            }
        }

        /// <summary>
        /// Registers a new item.
        /// </summary>
        /// <param name="item">The new  to register.</param>
        public virtual void RegisterNew( T item )
        {
            var hasChanges = this.HasPendingChanges;

            if ( this.DeletedItems.Contains( item ) )
                throw new InvalidOperationException( "A removed item cannot be registered as a new item." );
            else if ( this.UpdatedItems.Contains( item ) )
                throw new InvalidOperationException( "An existing item cannot be registered as a new item." );

            this.InsertedItems.Add( item );

            if ( this.HasPendingChanges != hasChanges )
                this.OnPropertyChanged( "HasPendingChanges" );
        }

        /// <summary>
        /// Registers a changed item.
        /// </summary>
        /// <param name="item">The changed <typeparamref name="T">item</typeparamref> to register.</param>
        public virtual void RegisterChanged( T item )
        {
            var hasChanges = this.HasPendingChanges;

            if ( !this.InsertedItems.Contains( item ) && !this.DeletedItems.Contains( item ) )
                this.UpdatedItems.Add( item );

            if ( this.HasPendingChanges != hasChanges )
                this.OnPropertyChanged( "HasPendingChanges" );
        }

        /// <summary>
        /// Registers a removed item.
        /// </summary>
        /// <param name="item">The removed <typeparamref name="T">item</typeparamref> to register.</param>
        public virtual void RegisterRemoved( T item )
        {
            var hasChanges = this.HasPendingChanges;
            var added = this.InsertedItems.Remove( item );

            this.UpdatedItems.Remove( item );

            if ( !added && !this.IsNew( item ) )
                this.DeletedItems.Add( item );

            if ( this.HasPendingChanges != hasChanges )
                this.OnPropertyChanged( "HasPendingChanges" );
        }

        /// <summary>
        /// Unregisters an item.
        /// </summary>
        /// <param name="item">The <typeparamref name="T">item</typeparamref> to unregister.</param>
        public virtual void Unregister( T item )
        {
            var hasChanges = this.HasPendingChanges;

            this.InsertedItems.Remove( item );
            this.UpdatedItems.Remove( item );
            this.DeletedItems.Remove( item );

            if ( this.HasPendingChanges != hasChanges )
                this.OnPropertyChanged( "HasPendingChanges" );
        }

        /// <summary>
        /// Rolls back all pending units of work.
        /// </summary>
        public virtual void Rollback()
        {
            this.AcceptChanges();
        }

        /// <summary>
        /// Commits all pending units of work asychronously.
        /// </summary>
        /// <param name="cancellationToken">The <see cref="CancellationToken">cancellation token</see> that can be used to cancel the operation.</param>
        /// <returns>A <see cref="Task">task</see> representing the commit operation.</returns>
        public abstract Task CommitAsync( CancellationToken cancellationToken );

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        /// <remarks>The <seealso cref="PropertyChanged"/> event can indicate all properties on the object have changed by using either
        /// <see langkeyword="null">null</see> or <see cref="F:String.Empty"/> as the property name in the <see cref="PropertyChangedEventArgs"/>.</remarks>
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
