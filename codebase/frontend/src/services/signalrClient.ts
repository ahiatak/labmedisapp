import * as signalR from '@microsoft/signalr'
import { getAccessToken } from './apiClient'

/**
 * SignalR connection to the LABMEDIS notification hub (contracts/notifications.md). One
 * shared connection per session, started once and reused by every subscriber (dashboard
 * widgets, notification center, etc.) — never poll, per Principle IX of the constitution.
 */

const HUB_URL = `${import.meta.env.VITE_API_BASE_URL ?? 'https://localhost:5443'}/hubs/notifications`

let connection: signalR.HubConnection | null = null

export function getNotificationHubConnection(): signalR.HubConnection {
  connection ??= new signalR.HubConnectionBuilder()
    .withUrl(HUB_URL, {
      accessTokenFactory: () => getAccessToken() ?? '',
    })
    .withAutomaticReconnect()
    .configureLogging(signalR.LogLevel.Warning)
    .build()

  return connection
}

export async function startNotificationHub(): Promise<signalR.HubConnection> {
  const hub = getNotificationHubConnection()
  if (hub.state === signalR.HubConnectionState.Disconnected) {
    await hub.start()
  }
  return hub
}

export async function stopNotificationHub(): Promise<void> {
  if (connection && connection.state !== signalR.HubConnectionState.Disconnected) {
    await connection.stop()
  }
}
