namespace Microsoft.DesignPatterns.Examples
{
    using System;
    using System.Diagnostics.Contracts;
    using System.Threading;
    using System.Threading.Tasks;

    [ContractClassFor( typeof( UnitOfWork<> ) )]
    internal abstract class UnitOfWorkContract<T> : UnitOfWork<T> where T : class
    {
        protected override bool IsNew( T item )
        {
            Contract.Requires( item != null, "item" );
            return default( bool );
        }

        public override Task CommitAsync( CancellationToken cancellationToken )
        {
            Contract.Ensures( Contract.Result<Task>() != null );
            return;
        }
    }
}
