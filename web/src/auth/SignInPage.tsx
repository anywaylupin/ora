import { useState, type SubmitEvent } from 'react'
import { Link, Navigate, useNavigate, useSearchParams } from 'react-router'
import { Button } from '../components/ui/Button'
import { Input } from '../components/ui/Input'
import { AuthLayout, FormErrors } from './AuthLayout'
import { signIn } from './authApi'
import { useSignedIn } from './useSignedIn'
import { formText } from '../lib/formData'

/**
 * Only same-site paths are followed after sign in, so a crafted link cannot bounce users to another site.
 */
function safeNext(next: string | null): string {
  return next?.startsWith('/') && !next.startsWith('//') ? next : '/'
}

export function SignInPage() {
  const signedIn = useSignedIn()
  const navigate = useNavigate()
  const [searchParams] = useSearchParams()
  const next = safeNext(searchParams.get('next'))
  const [errors, setErrors] = useState<string[]>([])
  const [pending, setPending] = useState(false)

  if (signedIn) return <Navigate to={next} replace />

  async function handleSubmit(event: SubmitEvent<HTMLFormElement>) {
    event.preventDefault()
    const form = new FormData(event.currentTarget)
    setPending(true)
    const result = await signIn(formText(form, 'email'), formText(form, 'password'))
    setPending(false)
    if (result.ok) await navigate(next, { replace: true })
    else setErrors(result.messages)
  }

  return (
    <AuthLayout title="Sign in to your account">
      <form className="space-y-4" onSubmit={(event) => void handleSubmit(event)}>
        <FormErrors messages={errors} />
        <Input label="Email" name="email" type="email" autoComplete="email" required />
        <Input
          label="Password"
          name="password"
          type="password"
          autoComplete="current-password"
          required
        />
        <Button type="submit" variant="primary" className="w-full" disabled={pending}>
          {pending ? 'Signing in' : 'Sign in'}
        </Button>
        <p className="text-center text-sm text-zinc-500">
          New to Ora?{' '}
          <Link to="/sign-up" className="font-medium text-indigo-600 dark:text-indigo-400">
            Create an account
          </Link>
        </p>
      </form>
    </AuthLayout>
  )
}
