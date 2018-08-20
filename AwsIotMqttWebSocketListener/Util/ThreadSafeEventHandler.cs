using System;

namespace AwsIotMqttWebSocketListener.Util {

	internal sealed class ThreadSafeEventHandler<TEventArgs> where TEventArgs : EventArgs {

		private readonly object m_lock = new object();
		private EventHandler<TEventArgs> m_handler = null;

		public void Add( EventHandler<TEventArgs> value ) {
			lock( m_lock ) {
				m_handler += value;
			}
		}

		public void Remove( EventHandler<TEventArgs> value ) {
			lock( m_lock ) {
				m_handler -= value;
			}
		}

		public void Invoke( object sender, TEventArgs args ) {

			EventHandler<TEventArgs> handler;
			lock( m_lock ) {
				handler = m_handler;
			}

			if( handler == null ) {
				return;
			}

			handler.Invoke( sender, args );
		}
	}
}
