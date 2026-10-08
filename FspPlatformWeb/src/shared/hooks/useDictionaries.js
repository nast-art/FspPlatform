import { useEffect, useState } from 'react'
import { dictionariesApi } from '../../api/dictionaries.api'

let cache = null
let inflight = null

export function useDictionaries() {
  const [data, setData] = useState(cache)
  const [loading, setLoading] = useState(!cache)
  const [error, setError] = useState(null)

  useEffect(() => {
    if (cache) return
    if (!inflight) {
      inflight = dictionariesApi.getAll().finally(() => {
        inflight = null
      })
    }
    inflight
      .then((d) => {
        cache = d
        setData(d)
      })
      .catch((e) => setError(e.message))
      .finally(() => setLoading(false))
  }, [])

  return { dictionaries: data, loading, error }
}